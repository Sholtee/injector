/********************************************************************************
* PermanentServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    //
    // !!!FIGYELEM!!! Csak belso hasznalatra, nem kell publikalni a Lifetime-ban
    //

    internal class PermanentServiceEntry : ProducibleServiceEntry
    {
        private readonly ConcurrentBag<IServiceReference> FInstances = new ConcurrentBag<IServiceReference>();

        public PermanentServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, factory, owner, customConverters)
        {
        }

        public PermanentServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, owner, customConverters)
        {
        }

        public PermanentServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, explicitArgs, owner, customConverters)
        {
        }

        [MethodImpl(MethodImplOptions.Synchronized)] // nem kene, csak kis paranoia
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

            FInstances.Add(reference);

            return true;
        }

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;

        public override Lifetime Lifetime { get; } = new PermanentLifetime();
    }
}