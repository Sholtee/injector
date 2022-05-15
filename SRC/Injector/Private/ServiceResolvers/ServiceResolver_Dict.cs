/********************************************************************************
* ServiceResolver_Dict.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    internal sealed class ServiceResolver_Dict: IServiceResolver
    {
        private sealed record CompositeKey(Type Interface, string? Name);

        private readonly IReadOnlyDictionary<CompositeKey, AbstractServiceEntry> FGenericEntries;

        //
        // Don't use ImmutableArray here since it is 2-3 times slower.
        //

        private volatile /*IReadOnly*/Dictionary<CompositeKey, Func<IInstanceFactory, object>> FResolvers;

        private readonly object FLock = new();

        private Func<IInstanceFactory, object> CreateResolver(AbstractServiceEntry entry)
        {
            if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
            {
                int slot = Slots++;
                return entry.Flags.HasFlag(ServiceEntryFlags.Shared)
                    ? fact => (fact.Super ?? fact).GetOrCreateInstance(entry, slot)
                    : fact => fact.GetOrCreateInstance(entry, slot);
            }
            else
                return entry.Flags.HasFlag(ServiceEntryFlags.Shared)
                    ? fact => (fact.Super ?? fact).CreateInstance(entry)
                    : fact => fact.CreateInstance(entry);
        }

        public const string Id = "dict";

        public int Slots { get; private set; }

        public ServiceResolver_Dict(IEnumerable<AbstractServiceEntry> entries)
        {
            Dictionary<CompositeKey, AbstractServiceEntry> genericEntries = new();
            Dictionary<CompositeKey, Func<IInstanceFactory, object>> resolvers = new();

            foreach (AbstractServiceEntry entry in entries)
            {
                CompositeKey key = new(entry.Interface, entry.Name);
                if (entry.Interface.IsGenericTypeDefinition)
                    genericEntries.Add(key, entry);
                else
                {
#if NETSTANDARD2_1_OR_GREATER
                    if (!resolvers.TryAdd(key, CreateResolver(entry)))
#else
                    try
                    {
                        resolvers.Add(key, CreateResolver(entry));
                    }
                    catch (ArgumentException)
#endif
                    {
                        InvalidOperationException ex = new(Resources.SERVICE_ALREADY_REGISTERED);
                        ex.Data[nameof(entry)] = entry;
                        throw ex;
                    }
                }
            }

            FGenericEntries = genericEntries;
            FResolvers = resolvers;
        }

        public Func<IInstanceFactory, object>? Get(Type iface, string? name)
        {
            CompositeKey key = new(iface, name);

            if (!FResolvers.TryGetValue(key, out Func<IInstanceFactory, object> resolver) && iface.IsConstructedGenericType)
            {
                lock (FLock)
                {
                    //
                    // FResolvers instance may be changed while we reached here
                    //

                    if (!FResolvers.TryGetValue(key, out resolver) && FGenericEntries.TryGetValue(new CompositeKey(iface.GetGenericTypeDefinition(), name), out AbstractServiceEntry genericEntry))
                    {
                        //
                        // Create a new resolver for the specializted entry
                        //

                        resolver = CreateResolver
                        (
                            genericEntry.Specialize(iface.GenericTypeArguments)
                        );

                        //
                        // In theory copying a dictionary is quick: https://github.com/dotnet/runtime/blob/c78bf2f522b4ce5a449faf6a38a0752b642a7f79/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs#L126
                        //

                        Dictionary<CompositeKey, Func<IInstanceFactory, object>> extendedResolvers = new(FResolvers);
                        extendedResolvers.Add(key, resolver);

                        FResolvers = extendedResolvers;
                    }
                }
            }

            return resolver;
        }
    }
}
