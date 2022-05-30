﻿/********************************************************************************
* ServiceResolverLookup_BuiltBTree.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class ServiceResolverLookup_BuiltBTree : ServiceResolverLookup_BTree
    {
        #region Private
        private static IEnumerable<Expression> Build<TResult>(ResolutionNode<TResult>? node, ParameterExpression iface, ParameterExpression name, ParameterExpression order, LabelTarget ret)
        {
            if (node is null)
            {
                yield return Expression.Return
                (
                    ret,
                    Expression.Default(typeof(TResult))
                );
                yield break;
            }

            //
            // order = CompareServiceIds(iface, name, RelatedEntry.Interface, RelatedEntry.Name);
            //

            yield return Expression.Assign
            (
                order,
                Expression.Invoke
                (
                    Expression.Constant((Func<long, string?, long, string?, int>) CompareServiceIds),
                    iface,
                    name,
                    Expression.Constant(node.Interface),
                    Expression.Constant(node.Name, typeof(string)) // typeof(string) is required as Name might be NULL
                )
            );

            //
            // if (order is null) return Result;
            //

            yield return Expression.IfThen
            (
                Expression.Equal(order, Expression.Constant(0)),
                Expression.Return
                (
                    ret,
                    Expression.Constant(node.Result, typeof(TResult))
                )
            );

            //
            // if (order < 0) {...}
            // else {...}
            //

            yield return Expression.IfThenElse
            (
                Expression.LessThan(order, Expression.Constant(0)),
                ifTrue: Expression.Block
                (
                    Build(node.Left as ResolutionNode<TResult>, iface, name, order, ret)
                ),
                ifFalse: Expression.Block
                (
                    Build(node.Right as ResolutionNode<TResult>, iface, name, order, ret)
                )
            );
        }

        private static Func<long, string?, TResult?> BuildSwitch<TResult>(RedBlackTree<ResolutionNode<TResult>> tree)
        {
            ParameterExpression
                iface = Expression.Parameter(typeof(long), nameof(iface)),
                name  = Expression.Parameter(typeof(string), nameof(name));

            LabelTarget ret = Expression.Label(typeof(TResult), nameof(ret));

            LabelExpression returnNull = Expression.Label(ret, Expression.Default(ret.Type));

            Expression body;
            if (tree.Root is null)
                body = returnNull;
            else
            {
                ParameterExpression order = Expression.Variable(typeof(int), nameof(order));
                List<Expression> block = new
                (
                    Build(tree.Root, iface, name, order, ret)
                );
                block.Add(returnNull);
                body = Expression.Block(new[] { order }, block);
            }

            Expression<Func<long, string?, TResult?>> resolver = Expression.Lambda<Func<long, string?, TResult?>>
            (
                body,
                iface,
                name
            );

            WriteLine($"Created resolver:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver.Compile();
        }

        private Func<long, string?, AbstractServiceEntry?> FGetGenericEntry;

        private volatile Func<long, string?, IServiceResolver?> FGetResolver;
        #endregion

        protected override void InitSwitches(IEnumerable<AbstractServiceEntry> entries)
        {
            base.InitSwitches(entries);

            FGetGenericEntry = BuildSwitch(FGetGenericEntrySwitch);
            FGetResolver = BuildSwitch(FGetResolverSwitch);
        }

        public new const string Id = "builtbtree";

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ServiceResolverLookup_BuiltBTree(IEnumerable<AbstractServiceEntry> entries, ServiceResolutionMode resolutionMode) : base(entries, resolutionMode)
        #pragma warning restore CS8618
        {
        }

        public override IServiceResolver? Get(Type iface, string? name)
        {
            long handle = (long) iface.TypeHandle.Value;

            IServiceResolver? resolver = FGetResolver(handle, name);
            if (resolver is null && iface.IsConstructedGenericType)
            {
                AbstractServiceEntry? genericEntry = FGetGenericEntry((long) iface.GetGenericTypeDefinition().TypeHandle.Value, name);
                if (genericEntry is not null)
                {
                    lock (FLock)
                    {
                        //
                        // Another thread might have registered the resolver while we reached here.
                        //

                        resolver = FGetResolver(handle, name);
                        if (resolver is null)
                        {
                            ResolutionNode<IServiceResolver> node = CreateServiceResolutionNode
                            (
                                genericEntry.Specialize(iface.GenericTypeArguments)
                            );

                            FGetResolverSwitch = FGetResolverSwitch.With(node);
                            FGetResolver = BuildSwitch(FGetResolverSwitch);

                            resolver = node.Result;
                        }
                    }
                }
            }

            return resolver;
        }
    }
}