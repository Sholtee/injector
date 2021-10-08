/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Threading;

using Microsoft.Collections.Extensions;

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
        // DictionarySlim-nek olyan kulcs kell ami megvalositja az IEquatable interface-t
        //

        private sealed record TypeWrap(Type Type);

        //
        // Ez NE egyetlen szotar legyen mert ott a neveket is szamon kene tartsuk amivel viszont
        // jelentosen lassulna a bejegyzes lekerdezes.
        //

        private readonly DictionarySlim<TypeWrap, AbstractServiceEntry>?[] FSpecializedEntries;

        public ServiceRegistry(IServiceCollection entries, ResolverBuilder? resolverBuilder = null, CancellationToken cancellation = default) : base(entries, resolverBuilder, cancellation)
        {
            FRegularEntries = new AbstractServiceEntry?[RegularEntryCount];
            FSpecializedEntries = new DictionarySlim<TypeWrap, AbstractServiceEntry>?[GenericEntryCount];
        }

        public ServiceRegistry(ServiceRegistryBase parent) : base(parent)
        {
            FRegularEntries = new AbstractServiceEntry?[RegularEntryCount];
            FSpecializedEntries = new DictionarySlim<TypeWrap, AbstractServiceEntry>?[GenericEntryCount];
        }

        public override AbstractServiceEntry ResolveGenericEntry(int slot, Type specializedInterface, AbstractServiceEntry originalEntry)
        {
            Debug.Assert(specializedInterface.IsConstructedGenericType, $"{nameof(specializedInterface)} must be a closed generic type");

            ref DictionarySlim<TypeWrap, AbstractServiceEntry>? specializedEntries = ref FSpecializedEntries[slot];
            if (specializedEntries is null)
                specializedEntries = new DictionarySlim<TypeWrap, AbstractServiceEntry>();

            ref AbstractServiceEntry specializedEntry = ref specializedEntries.GetOrAddValueRef(new TypeWrap(specializedInterface));
            if (specializedEntry is null)
            {
                ISupportsSpecialization supportsSpecialization = (ISupportsSpecialization) originalEntry;
                specializedEntry = supportsSpecialization.Specialize(this, specializedInterface.GenericTypeArguments);
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
