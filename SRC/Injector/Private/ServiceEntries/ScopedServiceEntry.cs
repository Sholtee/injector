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

    internal class ScopedServiceEntry : ProducibleServiceEntry
    {
        private readonly List<IServiceReference> FInstances = new(1); // max egy eleme lehet

        private ScopedServiceEntry(ScopedServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        protected override void SaveReference(IServiceReference serviceReference) => FInstances.Add(serviceReference);

        public ScopedServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {
            CheckNotDisposed();
            EnsureAppropriateReference(reference);

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

            //
            // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
            // beallitott erteket -> Minden egyes scope maximum egy sajat peldannyal rendelkezhet.
            //

            if (State.HasFlag(ServiceEntryStates.Built)) return false;

            //
            // Kulomben legyartjuk
            //

            base.SetInstance(reference);

            State |= ServiceEntryStates.Built;

            return true;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(target, nameof(target));

            var result = new ScopedServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        public override Lifetime Lifetime { get; } = Lifetime.Scoped;

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;
    }
}