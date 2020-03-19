/********************************************************************************
* ScopedServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes a scoped service entry.
    /// </summary>
    internal class ScopedServiceEntry : ProducibleServiceEntry
    {
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

        public override bool SetInstance(ServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            Ensure.Parameter.IsNotNull(reference, nameof(reference));
            Ensure.AreEqual(reference.RelatedServiceEntry, this, Resources.NOT_BELONGING_REFERENCE);

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");

            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);
            Ensure.IsNull(reference.Value, $"{nameof(reference)}.{nameof(reference.Value)}");

            CheckProducible(); // hivja az "Ensure.NotDisposed(this)"-t

            //
            // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
            // beallitott erteket.
            //

            if (Instance != null) return false;

            //
            // Kulomben legyartjuk. Elsokent a Factory-t hivjuk es Instance-nak csak sikeres visszateres
            // eseten adjunk erteket.
            //

            #pragma warning disable CS8602 // CheckProducible() ellenorzi h Factory letezik e
            reference.Value = Factory(relatedInjector, Interface);
            #pragma warning restore CS8602

            Instance = reference;

            return true;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            Ensure.Parameter.IsNotNull(target, nameof(target));
            Ensure.NotDisposed(this);

            var result = new ScopedServiceEntry(this, target);
            target.Add(result);
            return result;
        }
    }
}