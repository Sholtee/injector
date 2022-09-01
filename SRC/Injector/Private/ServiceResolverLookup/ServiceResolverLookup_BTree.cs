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

    internal class ServiceResolverLookup_BTree : ServiceResolverLookupBase
    {
        #region Private
        internal static int CompareServiceIds(CompositeKey x, CompositeKey y) => x.CompareTo(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RedBlackTreeNode<KeyValuePair<CompositeKey, TResult>> CreateNode<TResult>(AbstractServiceEntry entry, TResult value) => new
        (
            new KeyValuePair<CompositeKey, TResult>
            (
                new CompositeKey(entry),
                value
            )
        );
        #endregion

        #region Protected
        protected readonly RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> FGetGenericEntrySwitch =
            RedBlackTreeExtensions.Create<CompositeKey, AbstractServiceEntry>(CompareServiceIds);

        protected volatile RedBlackTree<KeyValuePair<CompositeKey, ServiceResolver>> FGetResolverSwitch =
            RedBlackTreeExtensions.Create<CompositeKey, ServiceResolver>(CompareServiceIds);

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

        protected override bool TryGetResolver(Type iface, string? name, out ServiceResolver resolver) => FGetResolverSwitch.TryGet
        (
            CompareServiceIds,
            new CompositeKey(iface, name),
            out resolver
        );

        protected override bool TryGetGenericEntry(Type iface, string? name, out AbstractServiceEntry genericEntry) => FGetGenericEntrySwitch.TryGet
        (
            CompareServiceIds,
            new CompositeKey(iface, name),
            out genericEntry
        );
        #endregion

        public const string Id = "btree";

        public ServiceResolverLookup_BTree(IEnumerable<AbstractServiceEntry> entries, ScopeOptions scopeOptions): base(entries, scopeOptions)
        {
        }
    }
}
