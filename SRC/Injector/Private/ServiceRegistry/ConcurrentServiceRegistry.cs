/********************************************************************************
* ConcurrentServiceRegistry.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ConcurrentServiceRegistry : ServiceRegistryBase
    {
        private sealed class EntryHolder
        {
            public AbstractServiceEntry? Value;
        }

        private readonly EntryHolder[] FRegularEntries;

        //
        // Ez NE egyetlen szotar legyen mert ott a neveket is szamon kene tartsuk amivel viszont
        // jelentosen lassulna a bejegyzes lekerdezes.
        //

        private readonly ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>[] FSpecializedEntries;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static T[] CreateArray<T>(Func<T> factory, int count)
        {
            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = factory();
            }

            return result;
        }

        public ConcurrentServiceRegistry(ISet<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, CancellationToken cancellation = default) : base(entries, resolverBuilder, cancellation)
        {
            FRegularEntries = CreateArray(() => new EntryHolder(), RegularEntryCount);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), GenericEntryCount);
        }

        public ConcurrentServiceRegistry(ConcurrentServiceRegistry parent) : base(parent)
        {
            FRegularEntries = CreateArray(() => new EntryHolder(), RegularEntryCount);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), GenericEntryCount);
        }

        public override AbstractServiceEntry ResolveGenericEntry(int slot, Type specializedInterface, AbstractServiceEntry originalEntry)
        {
            Debug.Assert(specializedInterface.IsConstructedGenericType, $"{nameof(specializedInterface)} must be a closed generic type");

            ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>> specializedEntries = FSpecializedEntries[slot];

            //
            // Lazy azert kell mert ha ugyanarra a kulcsra parhuzamosan kerul meghivasra a GetOrAdd() akkor a factory
            // tobbszor is meg lehet hivva (lasd MSDN).
            //

            return specializedEntries
                .GetOrAdd(specializedInterface, _ => new Lazy<AbstractServiceEntry>(Specialize, LazyThreadSafetyMode.ExecutionAndPublication))
                .Value;

            AbstractServiceEntry Specialize()
            {
                ISupportsSpecialization supportsSpecialization = (ISupportsSpecialization) originalEntry;
                return supportsSpecialization.Specialize(this, specializedInterface.GenericTypeArguments);
            }
        }

        public override AbstractServiceEntry ResolveRegularEntry(int slot, AbstractServiceEntry originalEntry)
        {
            EntryHolder holder = FRegularEntries[slot];

            if (holder.Value is null)
            {
                lock (holder) // entry csak egyszer legyen masolva
                {
                    if (holder.Value is null)
                    {
                        holder.Value = originalEntry.CopyTo(this);
                    }
                }
            }

            return holder.Value;
        }
    }
}
