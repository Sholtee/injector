﻿/********************************************************************************
* ServiceResolver.BinaryTree.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    using Primitives;
    using Primitives.Patterns;

    internal sealed class ServiceResolver_Tree: IServiceResolver
    {
        #region Private
        private static readonly StringComparer FStringComparer = StringComparer.Ordinal;

        internal static int CompareServiceIds(Type iface1, string? name1, Type iface2, string? name2)
        {
            //
            // IntPtr 32 bits long on x86 and 64 bits long on x64 systems however we have to return 
            // an Int32 on every systems -> Math.Sign()
            //

            int order = Math.Sign((long) iface1.TypeHandle.Value - (long) iface2.TypeHandle.Value);
            if (order is 0)
                //
                // StringComparer supports NULL despite it is not reflected by nullable annotation
                //

                order = FStringComparer.Compare(name1!, name2!);
            return order;
        }

        private interface IHasRelatedEntry
        {
            AbstractServiceEntry RelatedEntry { get; }
        }

        private sealed class NodeComparer : Singleton<NodeComparer>, IComparer<IHasRelatedEntry>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(IHasRelatedEntry x, IHasRelatedEntry y) => CompareServiceIds
            (
                x.RelatedEntry.Interface, x.RelatedEntry.Name, 
                y.RelatedEntry.Interface, y.RelatedEntry.Name
            );
        }

        private sealed class EntryResolutionNode : RedBlackTreeNode, IHasRelatedEntry
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
                        Expression.Constant((Func<Type, string?, Type, string?, int>)CompareServiceIds),
                        iface,
                        name,
                        Expression.Constant(RelatedEntry.Interface),
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

        private sealed class ServiceResolutionNode : RedBlackTreeNode, IHasRelatedEntry
        {
            private static readonly MethodInfo
                FCreateInstance = MethodInfoExtractor.Extract<IInstanceFactory>(fact => fact.CreateInstance(null!)),
                FGetOrCreateInstance = MethodInfoExtractor.Extract<IInstanceFactory>(fact => fact.GetOrCreateInstance(null!, 0));

            private static readonly PropertyInfo
                FSuper = PropertyInfoExtractor.Extract<IInstanceFactory, IInstanceFactory?>(fact => fact.Super);

            public ServiceResolutionNode(NodeColor color, AbstractServiceEntry relatedEntry, int? slot) : base(color)
            {
                RelatedEntry = relatedEntry;
                Slot = slot;
            }

            public ServiceResolutionNode(AbstractServiceEntry relatedEntry, int? slot) : this(NodeColor.Unspecified, relatedEntry, slot) { }

            public override RedBlackTreeNode ShallowClone() => new ServiceResolutionNode(Color, RelatedEntry, Slot);

            public AbstractServiceEntry RelatedEntry { get; }

            public int? Slot { get; }

            public IEnumerable<Expression> Build(ParameterExpression iface, ParameterExpression name, ParameterExpression svcFactory, ParameterExpression order, LabelTarget ret)
            {
                //
                // order = CompareServiceIds(iface, name, RelatedEntry.Interface, RelatedEntry.Name);
                //

                yield return Expression.Assign
                (
                    order,
                    Expression.Invoke
                    (
                        Expression.Constant((Func<Type, string?, Type, string?, int>) CompareServiceIds),
                        iface,
                        name,
                        Expression.Constant(RelatedEntry.Interface),
                        Expression.Constant(RelatedEntry.Name, typeof(string)) // typeof(string) is required as Name might be NULL
                    )
                );

                //
                // if (order is 0)
                //     return [svcFactory|svcFactory.Super ?? svcFactory].CreateInstance(RelatedEntry);
                //          OR
                //     return [svcFactory|svcFactory.Super ?? svcFactory].GetOrCreateInstance(RelatedEntry, Slot)
                //

                Expression instance = RelatedEntry.Flags.HasFlag(ServiceEntryFlags.Shared)
                    ? Expression.Coalesce
                    (
                        Expression.Property(svcFactory, FSuper),
                        svcFactory
                    )
                    : svcFactory;

                yield return Expression.IfThen
                (
                    Expression.Equal(order, Expression.Constant(0)),
                    Expression.Return
                    (
                        ret,
                        Slot is null
                            ? Expression.Call
                            (
                                instance,
                                FCreateInstance,
                                Expression.Constant(RelatedEntry)
                            )
                            : Expression.Call
                            (
                                instance,
                                FGetOrCreateInstance,
                                Expression.Constant(RelatedEntry),
                                Expression.Constant(Slot.Value)
                            )
                    )
                );

                //
                // if (order < 0) {...}
                // else {...}
                //

                Expression?
                    returnNull = Expression.Return(ret, Expression.Default(typeof(object))),
                    ifLess = Expression.Block
                    (
                        (Left as ServiceResolutionNode)?.Build(iface, name, svcFactory, order, ret) ?? new[] { returnNull }
                    ),
                    ifGreater = Expression.Block(
                        (Right as ServiceResolutionNode)?.Build(iface, name, svcFactory, order, ret) ?? new[] { returnNull }
                    );

                yield return Expression.IfThenElse
                (
                    Expression.LessThan(order, Expression.Constant(0)),
                    ifLess,
                    ifGreater
                );
            }
        }

        private static Expression BuildSwitchBody(IEnumerable<Expression>? @switch, ParameterExpression order, LabelTarget ret)
        {
            //
            // return null;
            //

            LabelExpression returnNull = Expression.Label(ret, Expression.Default(ret.Type));

            if (@switch is not null)
            {
                List<Expression> block = new(@switch);
                block.Add(returnNull);

                return Expression.Block(new[] { order }, block);
            }
            
            return returnNull;
        }

        private static Func<Type, string?, AbstractServiceEntry?> BuildSwitch(RedBlackTree<EntryResolutionNode> tree)
        {
            ParameterExpression
                iface = Expression.Parameter(typeof(Type), nameof(iface)),
                name  = Expression.Parameter(typeof(string), nameof(name)),
                order = Expression.Variable(typeof(int), nameof(order));

            LabelTarget ret = Expression.Label(typeof(AbstractServiceEntry));

            Expression<Func<Type, string?, AbstractServiceEntry?>> resolver = Expression.Lambda<Func<Type, string?, AbstractServiceEntry?>>
            (
                BuildSwitchBody
                (
                    tree.Root?.Build(iface, name, order, ret),
                    order,
                    ret
                ),
                iface,
                name
            );

            WriteLine($"Created resolver:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver.Compile();
        }

        private static Func<Type, string?, IInstanceFactory, object?> BuildSwitch(RedBlackTree<ServiceResolutionNode> tree)
        {
            ParameterExpression
                iface = Expression.Parameter(typeof(Type), nameof(iface)),
                name = Expression.Parameter(typeof(string), nameof(name)),
                fact = Expression.Parameter(typeof(IInstanceFactory), nameof(fact)),
                order = Expression.Variable(typeof(int), nameof(order));

            LabelTarget ret = Expression.Label(typeof(object));

            Expression<Func<Type, string?, IInstanceFactory, object?>> resolver = Expression.Lambda<Func<Type, string?, IInstanceFactory, object?>>
            (
                BuildSwitchBody
                (
                    tree.Root?.Build(iface, name, fact, order, ret),
                    order,
                    ret
                ),
                iface,
                name,
                fact
            );

            WriteLine($"Created resolver:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver.Compile();
        }

        private readonly Func<Type, string?, AbstractServiceEntry?> FGetGenericEntry;

        private readonly RedBlackTree<ServiceResolutionNode> FSwitch;

        private volatile Func<Type, string?, IInstanceFactory, object?> FInvokeFactory;

        private readonly object FLock = new();
        #endregion

        public int Slots { get; private set; }

        public ServiceResolver_Tree(IEnumerable<AbstractServiceEntry> entries)
        {
            RedBlackTree<EntryResolutionNode> getEntrySwitch = new(NodeComparer.Instance);
            FSwitch = new RedBlackTree<ServiceResolutionNode>(NodeComparer.Instance);

            foreach (AbstractServiceEntry entry in entries)
            {
                bool added = entry.Interface.IsGenericTypeDefinition
                    ? getEntrySwitch.Add
                    (
                        new EntryResolutionNode(entry)
                    )
                    : FSwitch.Add
                    (
                        new ServiceResolutionNode
                        (
                            entry,
                            entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance)
                                ? Slots++
                                : null
                        )
                    );
                Assert(added, $"Failed to register entry: {entry}");
            }

            FGetGenericEntry = BuildSwitch(getEntrySwitch);
            FInvokeFactory = BuildSwitch(FSwitch);
        }

        public object? Resolve(Type iface, string? name, IInstanceFactory instanceFactory)
        {
            object? result = FInvokeFactory(iface, name, instanceFactory);

            AbstractServiceEntry? genericEntry;

            if (result is null && iface.IsConstructedGenericType && (genericEntry = FGetGenericEntry(iface.GetGenericTypeDefinition(), name)) is not null)
            {
                lock (FLock)
                {
                    result = FInvokeFactory(iface, name, instanceFactory);
                    if (result is null)
                    {
                        bool added = FSwitch.Add
                        (
                            new ServiceResolutionNode
                            (
                                genericEntry.Specialize(iface.GenericTypeArguments),
                                genericEntry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance)
                                    ? Slots++
                                    : null
                            )
                        );
                        Assert(added, $"Failed to register entry: {genericEntry.Specialize(iface.GenericTypeArguments)}");
                        FInvokeFactory = BuildSwitch(FSwitch);

                        result = FInvokeFactory(iface, name, instanceFactory);
                    }
                }
            }

            return result;
        }
    }
}
