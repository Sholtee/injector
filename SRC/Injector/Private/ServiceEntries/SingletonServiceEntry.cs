/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    /// <summary>
    /// Describes a singleton service entry.
    /// </summary>
    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        public SingletonServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override bool SetInstance(IServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            EnsureAppropriateReference(reference);
            EnsureProducible();

            //
            // Singleton bejegyzeshez mindig sajat injector van letrehozva a deklaralo kontenerbol
            //

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer.Parent, Owner, Resources.INAPPROPRIATE_OWNERSHIP);         

            //
            // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
            // beallitott erteket.
            //

            if (Built) return false;

            //
            // Kulonben legyartjuk: 
            // - Elsokent a Factory-t hivjuk es Instance-nak csak sikeres visszateres eseten adjunk erteket.
            // - "Factory" biztosan nem NULL [lasd EnsureProducible()].
            //

            reference.Value = Factory!(relatedInjector, Interface);

            Instances = new[] { reference };

            return Built = true;
        }

        public override Lifetime Lifetime { get; } = Lifetime.Singleton;
    }
}