/********************************************************************************
* Resolver.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class Resolver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        #pragma warning disable CA1307 // Specify StringComparison for clarity
        private static int HashCombine(Type iface, string? name) => unchecked(iface.GetHashCode() ^ (name?.GetHashCode() ?? 0));
        #pragma warning restore CA1307

        //
        // - Dictionary performs much better against int keys.
        // - Don't use ImmutableArray here since it is 2-3 times slower.
        //

        private readonly IReadOnlyDictionary<int, AbstractServiceEntry> FGenericEntries;

        private IReadOnlyDictionary<int, Func<IInstanceFactory, object?>> FResolvers;

        private readonly object FLock = new();

        private Func<IInstanceFactory, object?> CreateResolver(AbstractServiceEntry entry)
        {
            if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
            {
                int slot = FSlots++;
                return fact => fact.GetOrCreateInstance(entry, slot);
            }
            else
                return fact => fact.CreateInstance(entry);
        }

        private int FSlots;
        public int Slots => FSlots;

        public Resolver(IEnumerable<AbstractServiceEntry> entries)
        {
            Dictionary<int, AbstractServiceEntry> genericEntries = new();
            Dictionary<int, Func<IInstanceFactory, object?>> resolvers = new();

            foreach (AbstractServiceEntry entry in entries)
            {
                int key = HashCombine(entry.Interface, entry.Name);
                if (entry.Interface.IsGenericTypeDefinition)
                    genericEntries.Add(key, entry);
                else
                    resolvers.Add(key, CreateResolver(entry));
            }

            FGenericEntries = genericEntries;
            FResolvers = resolvers;
        }

        public Func<IInstanceFactory, object?>? Get(Type iface, string? name)
        {
            int key = HashCombine(iface, name);

            if (!FResolvers.TryGetValue(key, out Func<IInstanceFactory, object?> resolver) && iface.IsConstructedGenericType)
            {
                lock (FLock)
                {
                    //
                    // FResolvers instance may be changed while we reached here
                    //

                    if (!FResolvers.TryGetValue(key, out resolver) && FGenericEntries.TryGetValue(HashCombine(iface.GetGenericTypeDefinition(), name), out AbstractServiceEntry genericEntry))
                    {
                        //
                        // Create a new resolver for the specializted entry
                        //

                        resolver = CreateResolver(genericEntry.Specialize(iface.GenericTypeArguments));

                        //
                        // In theory copying the dictionary is quick: https://github.com/dotnet/runtime/blob/c78bf2f522b4ce5a449faf6a38a0752b642a7f79/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs#L126
                        //

                        Dictionary<int, Func<IInstanceFactory, object?>> extendedResolvers = new((IDictionary<int, Func<IInstanceFactory, object?>>) FResolvers);
                        extendedResolvers.Add(key, resolver);

                        FResolvers = extendedResolvers;
                    }
                }
            }

            return resolver;
        }
    }
}
