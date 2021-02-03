/********************************************************************************
* PermanentServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    //
    // !!!FIGYELEM!!! Csak belso hasznalatra, nem kell publikalni a Lifetime-ban
    //

    internal class PermanentServiceEntry : ProducibleServiceEntry
    {
        public PermanentServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        public PermanentServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        public PermanentServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override bool SetInstance(IServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            EnsureAppropriateReference(reference);
            EnsureProducible();

            //
            // A bejegyzeshez mindig sajat injector van letrehozva a deklaralo kontenerbol
            //

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer.Parent, Owner, Resources.INAPPROPRIATE_OWNERSHIP);         

            //
            // Elsokent a Factory-t hivjuk es Instance-nak csak sikeres visszateres eseten adjunk erteket.
            // "Factory" biztosan nem NULL [lasd EnsureProducible()].
            //

            reference.Value = Factory!(relatedInjector, Interface);

            Instances = new[] { reference };

            return true;
        }

        public override Lifetime Lifetime { get; } = new PermanentLifetime();
    }
}