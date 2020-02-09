/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a singleton service entry.
    /// </summary>
    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        private AbstractServiceReference FInstance;

        //
        // Lock azert kell mert ez az entitas egyszerre tobb szerviz kollekcioban is szerepelhet
        // (SetInstance() lehet hivva parhuzamosan).
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

        public override AbstractServiceReference Instance => FInstance;

        public override bool SetInstance(AbstractServiceReference reference)
        {
            CheckProducible();

            //
            // Singleton bejegyzeshez mindig sajat injector van letrehozva a deklaralo kontenerbol
            //

            Debug.Assert(reference.RelatedInjector.UnderlyingContainer.Parent == Owner);

            //
            // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
            // beallitott erteket.
            //

            if (FInstance != null) return false;

            lock (FLock)
            {
                if (FInstance != null) return false;


                //
                // Kulomben legyartjuk. Elsokent a Factory-t hivjuk es Instance-nak csak sikeres visszateres
                // eseten adjunk erteket.
                //

                reference.Value = Factory(reference.RelatedInjector, Interface);
                FInstance = reference;
            }

            return true;
        }
    }
}