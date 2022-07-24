/********************************************************************************
* ServiceResolverLookup_BTree.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;
    using Primitives.Patterns;

    internal class ServiceResolverLookup_BTree : ServiceResolverLookupBase
    {
        #region Private
        private static readonly StringComparer FStringComparer = StringComparer.Ordinal;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private interface ICompositeKey
        {
            long Interface { get; }
            string? Name { get; }
        }

        private sealed class NodeComparer : Singleton<NodeComparer>, IComparer<ICompositeKey>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(ICompositeKey x, ICompositeKey y) => CompareServiceIds
            (
                x.Interface, x.Name,
                y.Interface, y.Name
            );
        }

        private static ResolutionNode<TResult> CreateNode<TResult>(AbstractServiceEntry key, TResult value) => new ResolutionNode<TResult>
        (
            (long) key.Interface.TypeHandle.Value,
            key.Name,
            value
        );

        private static bool TryGet<TResult>(ResolutionNode<TResult>? node, long iface, string? name, out TResult result)
        {
            result = default!;

            if (node is null)
                return false;

            int order = CompareServiceIds
            (
                iface,
                name,
                node.Interface,
                node.Name
            );

            if (order is 0)
            {
                result = node.Result;
                return true;
            }

            if (order < 0)
            {
                if (node.Left is not ResolutionNode<TResult> child)
                    return false;

                return TryGet(child, iface, name, out result);
            }

            if (order > 0)
            {
                if (node.Right is not ResolutionNode<TResult> child)
                    return false;

                return TryGet(child, iface, name, out result);
            }

            return false;
        }
        #endregion

        #region Protected
        protected readonly RedBlackTree<ResolutionNode<AbstractServiceEntry>> FGetGenericEntrySwitch = new(NodeComparer.Instance);

        protected volatile RedBlackTree<ResolutionNode<ServiceResolver>> FGetResolverSwitch = new(NodeComparer.Instance);

        protected sealed class ResolutionNode<TResult> : RedBlackTreeNode, ICompositeKey
        {
            public ResolutionNode(NodeColor color, long iface, string? name, TResult result) : base(color)
            {
                Interface = iface;
                Name = name;
                Result = result;
            }

            public ResolutionNode(long iface, string? name, TResult result) : this(NodeColor.Unspecified, iface, name, result) { }

            public override RedBlackTreeNode ShallowClone() => new ResolutionNode<TResult>(Color, Interface, Name, Result);

            public long Interface { get; }

            public string? Name { get; }

            public TResult Result { get; }
        }

        protected override bool TryAddResolver(ServiceResolver resolver) => FGetResolverSwitch.Add
        (
            CreateNode(resolver.RelatedEntry, resolver)
        );

        protected override void AddResolver(ServiceResolver resolver) => FGetResolverSwitch = FGetResolverSwitch.With
        (
            CreateNode(resolver.RelatedEntry, resolver)
        );

        protected override bool TryAddGenericEntry(AbstractServiceEntry entry) => FGetGenericEntrySwitch.Add
        (
            CreateNode(entry, entry)
        );

        protected override bool TryGetResolver(Type iface, string? name, out ServiceResolver resolver) => TryGet
        (
            FGetResolverSwitch.Root,
            (long) iface.TypeHandle.Value,
            name,
            out resolver
        );

        protected override bool TryGetGenericEntry(Type iface, string? name, out AbstractServiceEntry genericEntry) => TryGet
        (
            FGetGenericEntrySwitch.Root,
            (long) iface.TypeHandle.Value,
            name,
            out genericEntry
        );
        #endregion

        public const string Id = "btree";

        public ServiceResolverLookup_BTree(IEnumerable<AbstractServiceEntry> entries, ScopeOptions scopeOptions): base(entries, scopeOptions)
        {
        }
    }
}
