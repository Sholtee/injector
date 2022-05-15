/********************************************************************************
* ServiceResolver_BTree.cs                                                      *
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

    internal sealed class ServiceResolver_BTree: IServiceResolver
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

        private interface IResolutionNode
        {
            AbstractServiceEntry RelatedEntry { get; }

            IEnumerable<Expression> Build(ParameterExpression iface, ParameterExpression name, ParameterExpression order, LabelTarget ret);
        }

        private sealed class NodeComparer : Singleton<NodeComparer>, IComparer<IResolutionNode>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(IResolutionNode x, IResolutionNode y) => CompareServiceIds
            (
                (long) x.RelatedEntry.Interface.TypeHandle.Value, x.RelatedEntry.Name, 
                (long) y.RelatedEntry.Interface.TypeHandle.Value, y.RelatedEntry.Name
            );
        }

        private sealed class EntryResolutionNode : RedBlackTreeNode, IResolutionNode
        {
            public EntryResolutionNode(NodeColor color, AbstractServiceEntry relatedEntry) : base(color) => RelatedEntry = relatedEntry;

            public EntryResolutionNode(AbstractServiceEntry relatedEntry) : this(NodeColor.Unspecified, relatedEntry) { }

            public override RedBlackTreeNode ShallowClone() => new EntryResolutionNode(Color, RelatedEntry);

            public AbstractServiceEntry RelatedEntry { get; }

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
                        Expression.Constant((Func<long, string?, long, string?, int>) CompareServiceIds),
                        iface,
                        name,
                        Expression.Constant((long) RelatedEntry.Interface.TypeHandle.Value),
                        Expression.Constant(RelatedEntry.Name, typeof(string)) // typeof(string) is required as Name might be NULL
                    )
                );

                //
                // if (order is null) return RelatedEntry;
                //

                yield return Expression.IfThen
                (
                    Expression.Equal(order, Expression.Constant(0)),
                    Expression.Return
                    (
                        ret,
                        Expression.Constant(RelatedEntry)
                    )
                );

                //
                // if (order < 0) {...}
                // else {...}
                //

                Expression?
                    returnNull = Expression.Return(ret, Expression.Default(typeof(AbstractServiceEntry))),
                    ifLess = Expression.Block
                    (
                        (Left as EntryResolutionNode)?.Build(iface, name, order, ret) ?? new[] { returnNull }
                    ),
                    ifGreater = Expression.Block(
                        (Right as EntryResolutionNode)?.Build(iface, name, order, ret) ?? new[] { returnNull }
                    );

                yield return Expression.IfThenElse
                (
                    Expression.LessThan(order, Expression.Constant(0)),
                    ifLess,
                    ifGreater
                );
            }
        }

        private ServiceResolutionNode CreateServiceResolutionNode(AbstractServiceEntry entry)
        {
            Func<IInstanceFactory, object> factory;

            if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
            {
                int slot = Slots++;
                factory = entry.Flags.HasFlag(ServiceEntryFlags.Shared)
                    ? fact => (fact.Super ?? fact).GetOrCreateInstance(entry, slot)
                    : fact => fact.GetOrCreateInstance(entry, slot);
            }
            else
                factory = entry.Flags.HasFlag(ServiceEntryFlags.Shared)
                    ? fact => (fact.Super ?? fact).CreateInstance(entry)
                    : fact => fact.CreateInstance(entry);

            return new ServiceResolutionNode(entry, factory);
        }

        private sealed class ServiceResolutionNode : RedBlackTreeNode, IResolutionNode
        {
            public ServiceResolutionNode(NodeColor color, AbstractServiceEntry relatedEntry, Func<IInstanceFactory, object> factory) : base(color)
            {
                RelatedEntry = relatedEntry;
                Factory = factory;
            }

            public ServiceResolutionNode(AbstractServiceEntry relatedEntry, Func<IInstanceFactory, object> factory) : this(NodeColor.Unspecified, relatedEntry, factory) { }

            public override RedBlackTreeNode ShallowClone() => new ServiceResolutionNode(Color, RelatedEntry, Factory);

            public Func<IInstanceFactory, object> Factory { get; }

            public AbstractServiceEntry RelatedEntry { get; }

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
                        Expression.Constant((Func<long, string?, long, string?, int>) CompareServiceIds),
                        iface,
                        name,
                        Expression.Constant((long) RelatedEntry.Interface.TypeHandle.Value),
                        Expression.Constant(RelatedEntry.Name, typeof(string)) // typeof(string) is required as Name might be NULL
                    )
                );

                //
                // if (order is 0) return Factory;
                //

                yield return Expression.IfThen
                (
                    Expression.Equal(order, Expression.Constant(0)),
                    Expression.Return(ret, Expression.Constant(Factory))
                );

                //
                // if (order < 0) {...}
                // else {...}
                //

                Expression?
                    returnNull = Expression.Return(ret, Expression.Default(typeof(Func<IInstanceFactory, object>))),
                    ifLess = Expression.Block
                    (
                        (Left as ServiceResolutionNode)?.Build(iface, name, order, ret) ?? new[] { returnNull }
                    ),
                    ifGreater = Expression.Block(
                        (Right as ServiceResolutionNode)?.Build(iface, name, order, ret) ?? new[] { returnNull }
                    );

                yield return Expression.IfThenElse
                (
                    Expression.LessThan(order, Expression.Constant(0)),
                    ifLess,
                    ifGreater
                );
            }
        }

        private static Func<long, string?, TRet?> BuildSwitch<TRet, TNode>(RedBlackTree<TNode> tree) where TNode : RedBlackTreeNode, IResolutionNode
        {
            ParameterExpression
                iface = Expression.Parameter(typeof(long), nameof(iface)),
                name  = Expression.Parameter(typeof(string), nameof(name)),
                order = Expression.Variable(typeof(int), nameof(order));

            LabelTarget ret = Expression.Label(typeof(TRet));

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

            Expression<Func<long, string?, TRet?>> resolver = Expression.Lambda<Func<long, string?, TRet?>>(body, iface, name);

            WriteLine($"Created resolver:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver.Compile();
        }

        private readonly Func<long, string?, AbstractServiceEntry?> FGetGenericEntry;

        private readonly RedBlackTree<ServiceResolutionNode> FGetResolverSwitch;

        private volatile Func<long, string?, Func<IInstanceFactory, object>?> FGetResolver;

        private readonly object FLock = new();
        #endregion

        public const string Id = "btree";

        public int Slots { get; private set; }

        public ServiceResolver_BTree(IEnumerable<AbstractServiceEntry> entries)
        {
            RedBlackTree<EntryResolutionNode> getGenericEntrySwitch = new(NodeComparer.Instance);
            FGetResolverSwitch = new RedBlackTree<ServiceResolutionNode>(NodeComparer.Instance);

            foreach (AbstractServiceEntry entry in entries)
            {
                bool added = entry.Interface.IsGenericTypeDefinition
                    ? getGenericEntrySwitch.Add
                    (
                        new EntryResolutionNode(entry)
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

            FGetGenericEntry = BuildSwitch<AbstractServiceEntry, EntryResolutionNode>(getGenericEntrySwitch);
            FGetResolver = BuildSwitch<Func<IInstanceFactory, object>, ServiceResolutionNode>(FGetResolverSwitch);
        }

        public object? Resolve(Type iface, string? name, IInstanceFactory instanceFactory)
        {
            long handle = (long) iface.TypeHandle.Value;

            Func<IInstanceFactory, object>? resolver = FGetResolver(handle, name);

            AbstractServiceEntry? genericEntry;

            if (resolver is null && iface.IsConstructedGenericType && (genericEntry = FGetGenericEntry((long) iface.GetGenericTypeDefinition().TypeHandle.Value, name)) is not null)
            {
                lock (FLock)
                {
                    genericEntry = genericEntry.Specialize(iface.GenericTypeArguments);

                    int snapshot = Slots;

                    if (FGetResolverSwitch.Add(CreateServiceResolutionNode(genericEntry)))
                        FGetResolver = BuildSwitch<Func<IInstanceFactory, object>, ServiceResolutionNode>(FGetResolverSwitch);
                    else
                        //
                        // Another thread has already done this work. The slot number might be inconsistent now. Correct it!
                        //

                        Slots = snapshot;
                }
                resolver = FGetResolver(handle, name);
            }

            return resolver?.Invoke(instanceFactory);
        }
    }
}
