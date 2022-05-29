/********************************************************************************
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

        private readonly Func<long, string?, AbstractServiceEntry?> FGetGenericEntry;

        private volatile Func<long, string?, IServiceResolver?> FGetResolver;

        #endregion

        public new const string Id = "builtbtree";

        public ServiceResolverLookup_BuiltBTree(IEnumerable<AbstractServiceEntry> entries): base(entries)
        {
            FGetGenericEntry = BuildSwitch(FGetGenericEntrySwitch);
            FGetResolver = BuildSwitch(FGetResolverSwitch);
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

                            FGetResolverSwitch.Add(node);
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
