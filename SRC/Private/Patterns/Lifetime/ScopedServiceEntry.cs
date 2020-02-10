/********************************************************************************
* ScopedServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a scoped service entry.
    /// </summary>
    internal class ScopedServiceEntry : ProducibleServiceEntry
    {
        private AbstractServiceReference FInstance;

        private ScopedServiceEntry(ScopedServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Scoped, factory, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string name, Type implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string name, ITypeResolver implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public override AbstractServiceReference Instance => FInstance;

        public override bool SetInstance(AbstractServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            CheckProducible();

            //
            // Az injectornak kell lennie a bejegyzes tulajdonosanak.
            //

            Debug.Assert(reference.RelatedInjector.UnderlyingContainer == Owner);

            //
            // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
            // beallitott erteket.
            //

            if (FInstance != null) return false;

            //
            // Kulomben legyartjuk. Elsokent a Factory-t hivjuk es Instance-nak csak sikeres visszateres
            // eseten adjunk erteket.
            //

            reference.Value = Factory(reference.RelatedInjector, Interface);
            FInstance = reference;

            return true;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            CheckDisposed();

            var result = new ScopedServiceEntry(this, target);
            target.Add(result);
            return result;
        }
    }
}