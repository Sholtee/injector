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
        private sealed record CompositeKey(Type Interface, string? Name);

        private readonly /*IReadOnly*/Dictionary<CompositeKey, AbstractServiceEntry> FGenericEntries = new();

        //
        // Don't use ImmutableArray here since it is 2-3 times slower.
        //

        private volatile /*IReadOnly*/Dictionary<CompositeKey, IServiceResolver> FResolvers = new();

        public const string Id = "dict";

        public ServiceResolverLookup_Dict(IEnumerable<AbstractServiceEntry> entries, ScopeOptions scopeOptions) : base(scopeOptions)
        {
            foreach (AbstractServiceEntry entry in entries)
            {
                if (entry.Interface.IsGenericTypeDefinition)
                    RegisterEntry(FGenericEntries, entry, entry);
                else
                    RegisterEntry(FResolvers, entry, CreateResolver(entry));
            }

            InitResolvers(FResolvers.Values);

            static void RegisterEntry<TValue>(Dictionary<CompositeKey, TValue> target, AbstractServiceEntry entry, TValue value)
            {
                CompositeKey key = new(entry.Interface, entry.Name);
#if NETSTANDARD2_1_OR_GREATER
                if (!target.TryAdd(key, value))
#else
                try
                {
                    target.Add(key, value);
                }
                catch (ArgumentException)
#endif
                {
                    ServiceErrors.AlreadyRegistered(entry);
                }
            }
        }

        public override IServiceResolver? Get(Type iface, string? name)
        {
            CompositeKey key = new(iface, name);

            if (!FResolvers.TryGetValue(key, out IServiceResolver resolver) && iface.IsConstructedGenericType && FGenericEntries.TryGetValue(new CompositeKey(iface.GetGenericTypeDefinition(), name), out AbstractServiceEntry genericEntry))
            {
                lock (FLock)
                {
                    //
                    // FResolvers instance may be changed while we reached here
                    //

                    if (!FResolvers.TryGetValue(key, out resolver))
                    {
                        //
                        // Create a new resolver for the specializted entry
                        //

                        resolver = CreateResolver
                        (
                            genericEntry.Specialize(iface.GenericTypeArguments)
                        );

                        //
                        // In theory copying a dictionary is quick:
                        // https://github.com/dotnet/runtime/blob/c78bf2f522b4ce5a449faf6a38a0752b642a7f79/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs#L126
                        //

                        Dictionary<CompositeKey, IServiceResolver> extendedResolvers = new(FResolvers);
                        extendedResolvers.Add(key, resolver);

                        FResolvers = extendedResolvers;
                    }
                }
            }

            return resolver;
        }
    }
}
