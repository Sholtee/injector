/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed class ServiceResolver: IServiceResolver
    {
        private sealed class CompositeKey
        {
            public readonly Type Interface;

            public readonly string? Name;

            public readonly int HashCode;

            public CompositeKey(Type iface, string? name)
            {
                Interface = iface;
                Name = name;
                #pragma warning disable CA1307 // Specify StringComparison for clarity
                HashCode = iface.GetHashCode() ^ (name?.GetHashCode() ?? 0);
                #pragma warning restore CA1307
            }
        }

        private sealed class CompositeKeyComparer : Singleton<CompositeKeyComparer>, IEqualityComparer<CompositeKey>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(CompositeKey x, CompositeKey y) => x.Interface == y.Interface && x.Name == y.Name;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(CompositeKey obj) => obj.HashCode;
        }

        private readonly IReadOnlyDictionary<CompositeKey, AbstractServiceEntry> FGenericEntries;

        //
        // Don't use ImmutableArray here since it is 2-3 times slower.
        //

        private /*IReadOnly*/Dictionary<CompositeKey, Func<IInstanceFactory, object?>> FResolvers;

        private readonly object FLock = new();

        private Func<IInstanceFactory, object?> CreateResolver(AbstractServiceEntry entry)
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

        public int Slots { get; private set; }

        public ServiceResolver(IEnumerable<AbstractServiceEntry> entries)
        {
            Dictionary<CompositeKey, AbstractServiceEntry> genericEntries = new(CompositeKeyComparer.Instance);
            Dictionary<CompositeKey, Func<IInstanceFactory, object?>> resolvers = new(CompositeKeyComparer.Instance);

            foreach (AbstractServiceEntry entry in entries)
            {
                CompositeKey key = new(entry.Interface, entry.Name);
                if (entry.Interface.IsGenericTypeDefinition)
                    genericEntries.Add(key, entry);
                else
                    resolvers.Add(key, CreateResolver(entry));
            }

            FGenericEntries = genericEntries;
            FResolvers = resolvers;
        }

        public object? Resolve(Type iface, string? name, IInstanceFactory instanceFactory)
        {
            CompositeKey key = new(iface, name);

            if (!FResolvers.TryGetValue(key, out Func<IInstanceFactory, object?> resolver) && iface.IsConstructedGenericType)
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

                        resolver = CreateResolver(genericEntry.Specialize(iface.GenericTypeArguments));

                        //
                        // In theory copying a dictionary is quick: https://github.com/dotnet/runtime/blob/c78bf2f522b4ce5a449faf6a38a0752b642a7f79/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs#L126
                        //

                        Dictionary<CompositeKey, Func<IInstanceFactory, object?>> extendedResolvers = new(FResolvers, CompositeKeyComparer.Instance);
                        extendedResolvers.Add(key, resolver);

                        FResolvers = extendedResolvers;
                    }
                }
            }

            return resolver?.Invoke(instanceFactory);
        }
    }
}
