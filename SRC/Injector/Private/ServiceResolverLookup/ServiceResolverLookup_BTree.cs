/********************************************************************************
* ServiceResolverLookup_BTree.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    using Primitives;
    using Primitives.Patterns;

    internal sealed class ServiceResolverLookup_BTree : ServiceResolverLookupBase
    {
        #region Private
        private static readonly StringComparer FStringComparer = StringComparer.Ordinal;

        internal static int CompareServiceIds(long iface1, string? name1, long iface2, string? name2)
        {
            //
            // We have to return Int32 -> Math.Sign()
            //

            int order = Math.Sign(iface1 - iface2);
            if (order is 0)
                //
                // StringComparer supports NULL despite it is not reflected by nullable annotation
                //

                order = FStringComparer.Compare(name1!, name2!);
            return order;
        }

        private interface IHasAssociatedEntry
        {
            AbstractServiceEntry RelatedEntry { get; }
        }

        private sealed class NodeComparer : Singleton<NodeComparer>, IComparer<IHasAssociatedEntry>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(IHasAssociatedEntry x, IHasAssociatedEntry y) => CompareServiceIds
            (
                (long) x.RelatedEntry.Interface.TypeHandle.Value, x.RelatedEntry.Name,
                (long) y.RelatedEntry.Interface.TypeHandle.Value, y.RelatedEntry.Name
            );
        }

        private sealed class ResolutionNode<TResult> : RedBlackTreeNode, IHasAssociatedEntry
        {
            public ResolutionNode(NodeColor color, AbstractServiceEntry relatedEntry, TResult result) : base(color)
            {
                RelatedEntry = relatedEntry;
                Result = result;
            }

            public ResolutionNode(AbstractServiceEntry relatedEntry, TResult result) : this(NodeColor.Unspecified, relatedEntry, result) { }

            public override RedBlackTreeNode ShallowClone() => new ResolutionNode<TResult>(Color, RelatedEntry, Result);

            public AbstractServiceEntry RelatedEntry { get; }

            public TResult Result { get; }

            public IEnumerable<Expression> Build(ParameterExpression iface, ParameterExpression name, ParameterExpression order, LabelTarget ret)
            {
                //
                // order = CompareServiceIds(iface, name, RelatedEntry.Interface, RelatedEntry.Name);
                //

                yield return Expression.Assign
                (
                    order,
                    Expression.Invoke
                    (
                        Expression.Constant((Func<long, string?, long, string?, int>)CompareServiceIds),
                        iface,
                        name,
                        Expression.Constant((long)RelatedEntry.Interface.TypeHandle.Value),
                        Expression.Constant(RelatedEntry.Name, typeof(string)) // typeof(string) is required as Name might be NULL
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
                        Expression.Constant(Result)
                    )
                );

                //
                // if (order < 0) {...}
                // else {...}
                //

                Expression?
                    returnNull = Expression.Return(ret, Expression.Default(typeof(TResult))),
                    ifLess = Expression.Block
                    (
                        (Left as ResolutionNode<TResult>)?.Build(iface, name, order, ret) ?? new[] { returnNull }
                    ),
                    ifGreater = Expression.Block(
                        (Right as ResolutionNode<TResult>)?.Build(iface, name, order, ret) ?? new[] { returnNull }
                    );

                yield return Expression.IfThenElse
                (
                    Expression.LessThan(order, Expression.Constant(0)),
                    ifLess,
                    ifGreater
                );
            }
        }

        private ResolutionNode<Func<IInstanceFactory, object>> CreateServiceResolutionNode(AbstractServiceEntry entry) => new ResolutionNode<Func<IInstanceFactory, object>>
        (
            entry,
            CreateResolver(entry)
        );

        private static Func<long, string?, TResult?> BuildSwitch<TResult>(RedBlackTree<ResolutionNode<TResult>> tree)
        {
            ParameterExpression
                iface = Expression.Parameter(typeof(long), nameof(iface)),
                name  = Expression.Parameter(typeof(string), nameof(name)),
                order = Expression.Variable(typeof(int), nameof(order));

            LabelTarget ret = Expression.Label(typeof(TResult));

            LabelExpression returnNull = Expression.Label(ret, Expression.Default(ret.Type));

            IEnumerable<Expression>? @switch = tree.Root?.Build(iface, name, order, ret);

            Expression body;

            if (@switch is not null)
            {
                List<Expression> block = new(@switch);
                block.Add(returnNull);

                body = Expression.Block(new[] { order }, block);
            }
            else body = returnNull;

            Expression<Func<long, string?, TResult?>> resolver = Expression.Lambda<Func<long, string?, TResult?>>(body, iface, name);

            WriteLine($"Created resolver:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver.Compile();
        }

        private readonly Func<long, string?, AbstractServiceEntry?> FGetGenericEntry;

        private readonly RedBlackTree<ResolutionNode<Func<IInstanceFactory, object>>> FGetResolverSwitch;

        private volatile Func<long, string?, Func<IInstanceFactory, object>?> FGetResolver;

        #endregion

        public const string Id = "btree";

        public ServiceResolverLookup_BTree(IEnumerable<AbstractServiceEntry> entries)
        {
            RedBlackTree<ResolutionNode<AbstractServiceEntry>> getGenericEntrySwitch = new(NodeComparer.Instance);
            FGetResolverSwitch = new RedBlackTree<ResolutionNode<Func<IInstanceFactory, object>>>(NodeComparer.Instance);

            foreach (AbstractServiceEntry entry in entries)
            {
                bool added = entry.Interface.IsGenericTypeDefinition
                    ? getGenericEntrySwitch.Add
                    (
                        new ResolutionNode<AbstractServiceEntry>(entry, entry)
                    )
                    : FGetResolverSwitch.Add
                    (
                        CreateServiceResolutionNode(entry)
                    );
                if (!added)
                {
                    InvalidOperationException ex = new(Resources.SERVICE_ALREADY_REGISTERED);
                    ex.Data[nameof(entry)] = entry;
                    throw ex;
                }
            }

            FGetGenericEntry = BuildSwitch(getGenericEntrySwitch);
            FGetResolver = BuildSwitch(FGetResolverSwitch);
        }

        public override Func<IInstanceFactory, object>? Get(Type iface, string? name)
        {
            long handle = (long) iface.TypeHandle.Value;

            Func<IInstanceFactory, object>? resolver = FGetResolver(handle, name);
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
                            ResolutionNode<Func<IInstanceFactory, object>> node = CreateServiceResolutionNode
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
