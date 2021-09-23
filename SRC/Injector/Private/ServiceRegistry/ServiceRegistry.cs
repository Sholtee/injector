/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

    internal class ServiceRegistry : ServiceRegistryBase
    {
        private readonly AbstractServiceEntry?[] FRegularEntries;

        //
        // Ez NE egyetlen szotar legyen mert ott a neveket is szamon kene tartsuk amivel viszont
        // jelentosen lassulna a bejegyzes lekerdezes.
        //

        private readonly HybridDictionary?[] FSpecializedEntries;

        public ServiceRegistry(ISet<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, CancellationToken cancellation = default) : base(entries, resolverBuilder, cancellation)
        {
            FRegularEntries = new AbstractServiceEntry?[RegularEntryCount];
            FSpecializedEntries = new HybridDictionary?[GenericEntryCount];
        }

        public ServiceRegistry(ServiceRegistryBase parent) : base(parent)
        {
            FRegularEntries = new AbstractServiceEntry?[RegularEntryCount];
            FSpecializedEntries = new HybridDictionary?[GenericEntryCount];
        }

        public override AbstractServiceEntry ResolveGenericEntry(int slot, Type specializedInterface, AbstractServiceEntry originalEntry)
        {
            Debug.Assert(specializedInterface.IsConstructedGenericType, $"{nameof(specializedInterface)} must be a closed generic type");

            ref HybridDictionary? specializedEntries = ref FSpecializedEntries[slot];
            if (specializedEntries is null)
                specializedEntries = new HybridDictionary();

            AbstractServiceEntry? specializedEntry = (AbstractServiceEntry?) specializedEntries[specializedInterface];

            if (specializedEntry is null)
            {
                ISupportsSpecialization supportsSpecialization = (ISupportsSpecialization) originalEntry;

                specializedEntry = supportsSpecialization.Specialize(this, specializedInterface.GenericTypeArguments);
                specializedEntries[specializedInterface] = specializedEntry;
            }

            return specializedEntry;
        }

        public override AbstractServiceEntry ResolveRegularEntry(int slot, AbstractServiceEntry originalEntry)
        {
            ref AbstractServiceEntry? value = ref FRegularEntries[slot];
            if (value is null) 
                value = originalEntry.CopyTo(this);

            return value;
        }
    }
}
