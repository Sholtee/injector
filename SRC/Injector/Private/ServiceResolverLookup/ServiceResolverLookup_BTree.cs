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
        private static readonly StringComparer FStringComparer = StringComparer.Ordinal;

        internal sealed record CompositeKey(long Interface, string? Name)
        {
            public CompositeKey(Type iface, string? name): this((long) iface.TypeHandle.Value, name)
            {
            }
        }

        internal static int CompareServiceIds(CompositeKey x, CompositeKey y)
        {
            //
            // We have to return Int32 -> Math.Sign()
            //

            int order = Math.Sign(x.Interface - y.Interface);
            if (order is 0)
                //
                // StringComparer supports NULL despite it is not reflected by nullable annotation
                //

                order = FStringComparer.Compare(x.Name, y.Name);
            return order;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RedBlackTreeNode<KeyValuePair<CompositeKey, TResult>> CreateNode<TResult>(AbstractServiceEntry key, TResult value) => new
        (
            new KeyValuePair<CompositeKey, TResult>
            (
                new CompositeKey(key.Interface, key.Name),
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
