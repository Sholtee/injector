/********************************************************************************
* ConcurrentServiceResolverLookup.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Resolver lookup being shared among scopes.
    /// </summary>
    internal sealed class ConcurrentServiceResolverLookup: IServiceResolverLookup
    {
        private volatile IReadOnlyLookup<ServiceResolver> FResolvers;

        private readonly IReadOnlyLookup<AbstractServiceEntry> FGenericEntries;

        private readonly IGraphBuilder FGraphBuilder;

        private readonly object FLock = new();

        private int FSlots;

        public ConcurrentServiceResolverLookup(IReadOnlyLookup<AbstractServiceEntry> genericEntries, IReadOnlyLookup<ServiceResolver> resolvers, Func<IServiceResolverLookup, IGraphBuilder> graphBuilderFactory)
        {
            FGenericEntries = genericEntries;
            FResolvers = resolvers;
            FGraphBuilder = graphBuilderFactory(this);
        }

        public int Slots => FSlots;

        public ServiceResolver? Get(Type iface, string? name)
        {
            CompositeKey key = new(iface, name);
            if (FResolvers.TryGet(key, out ServiceResolver resolver))
                return resolver;

            if (!iface.IsConstructedGenericType)
                return null;

            CompositeKey genericKey = new(iface.GetGenericTypeDefinition(), name);
            if (!FGenericEntries.TryGet(genericKey, out AbstractServiceEntry genericEntry))
                return null;

            lock (FLock)
            {
                //
                // Another thread might have registered the resolver while we reached here.
                //

                if (FResolvers.TryGet(key, out resolver))
                    return resolver;

                genericEntry = genericEntry.Specialize(iface.GenericTypeArguments);

                //
                // Build the entry before it gets exposed (before the AddResolver call).
                //

                FGraphBuilder.Build(genericEntry);

                FResolvers = FResolvers.Add
                (
                    key,
                    resolver = new ServiceResolver
                    (
                        genericEntry,
                        genericEntry.CreateResolver(ref FSlots)
                    )
                );

                return resolver;
            }
        }
    }
}
