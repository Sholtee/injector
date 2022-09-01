/********************************************************************************
* ServiceResolverLookup_Dict.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceResolverLookup_Dict: ServiceResolverLookupBase
    {
        private readonly Dictionary<CompositeKey, AbstractServiceEntry> FGenericEntries = new();

        private volatile Dictionary<CompositeKey, ServiceResolver> FResolvers = new();

        private static bool TryAdd<TValue>(Dictionary<CompositeKey, TValue> target, AbstractServiceEntry entry, TValue value)
        {
            CompositeKey key = new(entry);
#if NETSTANDARD2_1_OR_GREATER
            return target.TryAdd(key, value);
#else
            try
            {
                target.Add(key, value);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
#endif
        }

        protected override bool TryAddGenericEntry(AbstractServiceEntry entry) => TryAdd(FGenericEntries, entry, entry);

        protected override void AddResolver(ServiceResolver resolver)
        {
            Dictionary<CompositeKey, ServiceResolver> extendedResolvers = new(FResolvers);          
            
            extendedResolvers.Add
            (
                new CompositeKey(resolver.RelatedEntry),
                resolver
            );
            
            FResolvers = extendedResolvers;
        }

        protected override bool TryAddResolver(ServiceResolver resolver) =>
            TryAdd(FResolvers, resolver.RelatedEntry, resolver);

        protected override bool TryGetResolver(Type iface, string? name, out ServiceResolver resolver) =>
            FResolvers.TryGetValue(new CompositeKey(iface, name), out resolver);

        protected override bool TryGetGenericEntry(Type iface, string? name, out AbstractServiceEntry genericEntry) =>
            FGenericEntries.TryGetValue(new CompositeKey(iface, name), out genericEntry);

        public const string Id = "dict";

        public ServiceResolverLookup_Dict(IEnumerable<AbstractServiceEntry> entries, ScopeOptions scopeOptions) : base(entries, scopeOptions)
        {
        }
    }
}
