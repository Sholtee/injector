/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a singleton service entry.
    /// </summary>
    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        //
        // Lock azert kell mert ez az entitas egyszerre tobb szerviz kollekcioban is szerepelhet ->
        // SetInstance() elmeletileg lehet hivva parhuzamosan (gyakorlatilag nem de biztos ami tuti
        // marad).
        //

        private readonly object FLock = new object();

        public SingletonServiceEntry(Type @interface, string name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Singleton, factory, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string name, Type implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Singleton, implementation, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string name, ITypeResolver implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Singleton, implementation, owner)
        {
        }

        public override bool SetInstance(ServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            CheckProducible();

            Ensure.Parameter.IsNotNull(reference, nameof(reference));
            Ensure.AreEqual(reference.RelatedServiceEntry, this);
            Ensure.IsNull(reference.Value, $"{nameof(reference)}.{nameof(reference.Value)}");

            //
            // Singleton bejegyzeshez mindig sajat injector van letrehozva a deklaralo kontenerbol
            //

            Ensure.AreEqual(reference.RelatedInjector.UnderlyingContainer.Parent, Owner);

            //
            // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
            // beallitott erteket.
            //

            if (Instance != null) return false;

            lock (FLock)
            {
                if (Instance != null) return false;

                //
                // Kulomben legyartjuk. Elsokent a Factory-t hivjuk es Instance-nak csak sikeres visszateres
                // eseten adjunk erteket.
                //

                reference.Value = Factory(reference.RelatedInjector, Interface);
                Instance = reference;
            }

            return true;
        }
    }
}