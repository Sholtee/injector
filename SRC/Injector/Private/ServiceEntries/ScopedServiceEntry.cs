/********************************************************************************
* ScopedServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    /// <summary>
    /// Describes a scoped service entry.
    /// </summary>
    internal class ScopedServiceEntry : ProducibleServiceEntry
    {
        private readonly List<IServiceReference> FInstances = new List<IServiceReference>(1); // max egy eleme lehet

        private ScopedServiceEntry(ScopedServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, factory, owner, customConverters)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, owner, customConverters)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, explicitArgs, owner, customConverters)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {
            EnsureAppropriateReference(reference);
            EnsureProducible();

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

            //
            // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
            // beallitott erteket.
            //

            if (Built) return false;

            //
            // Kulomben legyartjuk: 
            // - Elsokent a Factory-t hivjuk es Instance-nak csak sikeres visszateres eseten adjunk erteket.
            // - "Factory" biztosan nem NULL [lasd EnsureProducible()].
            //

            reference.Value = Factory!(relatedInjector, Interface);
            FInstances.Add(reference);

            return Built = true;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(target, nameof(target));

            var result = new ScopedServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;

        public override Lifetime Lifetime { get; } = Lifetime.Scoped;
    }
}