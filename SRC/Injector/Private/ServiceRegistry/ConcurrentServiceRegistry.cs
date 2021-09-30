/********************************************************************************
* ConcurrentServiceRegistry.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    //                                        !!!FIGYELEM!!!
    //
    // Ez az osztaly kozponti komponens, ezert minden modositast korultekintoen, a teljesitmenyt szem elott tartva
    // kell elvegezni:
    // - nincs Sysmte.Linq
    // - nincs System.Reflection
    // - mindig futtassuk a teljesitmeny teszteket (is) hogy a hatekonysag nem romlott e
    //

    internal class ConcurrentServiceRegistry : ServiceRegistryBase
    {
        private sealed class EntryHolder
        {
            public AbstractServiceEntry? Value;
        }

        private readonly Func<EntryHolder[]> FRegularEntriesFactory;

        private readonly EntryHolder[] FRegularEntries;

        private readonly Func<ConcurrentDictionary<Type, AbstractServiceEntry>[]> FSpecializedEntriesFactory;

        //
        // Ez NE egyetlen szotar legyen mert ott a neveket is szamon kene tartsuk amivel viszont
        // jelentosen lassulna a bejegyzes lekerdezes.
        //

        private readonly ConcurrentDictionary<Type, AbstractServiceEntry>[] FSpecializedEntries;

        public ConcurrentServiceRegistry(ISet<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, CancellationToken cancellation = default) : base(entries, resolverBuilder, cancellation)
        {
            FRegularEntriesFactory = ArrayFactory<EntryHolder>.Create(RegularEntryCount);
            FRegularEntries = FRegularEntriesFactory();

            FSpecializedEntriesFactory = ArrayFactory<ConcurrentDictionary<Type, AbstractServiceEntry>>.Create(GenericEntryCount);
            FSpecializedEntries = FSpecializedEntriesFactory();
        }

        public ConcurrentServiceRegistry(ConcurrentServiceRegistry parent) : base(parent)
        {
            FRegularEntriesFactory = parent.FRegularEntriesFactory;
            FRegularEntries = FRegularEntriesFactory();

            FSpecializedEntriesFactory = parent.FSpecializedEntriesFactory;
            FSpecializedEntries = FSpecializedEntriesFactory();
        }

        public override AbstractServiceEntry ResolveGenericEntry(int slot, Type specializedInterface, AbstractServiceEntry originalEntry)
        {
            Debug.Assert(specializedInterface.IsConstructedGenericType, $"{nameof(specializedInterface)} must be a closed generic type");

            ConcurrentDictionary<Type, AbstractServiceEntry> specializedEntries = FSpecializedEntries[slot];

            //
            // Megjegyzes: Ha ugyanarra a kulcsra parhuzamosan kerul meghivasra a GetOrAdd() akkor a factory
            // tobbszor is lefuthat (lasd MSDN) de itt ez nem okoz gondot.
            //

            return specializedEntries.GetOrAdd(specializedInterface, _ =>
            {
                ISupportsSpecialization supportsSpecialization = (ISupportsSpecialization) originalEntry;
                return supportsSpecialization.Specialize(this, specializedInterface.GenericTypeArguments);
            });
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
