/********************************************************************************
* PermanentServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#if false
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;
    using Properties;

    //
    // !!!FIGYELEM!!! Csak belso hasznalatra, nem kell publikalni a Lifetime-ban
    //

    internal class PermanentServiceEntry : ProducibleServiceEntry
    {
        private readonly ConcurrentBag<IServiceReference> FInstances = new();

        private readonly ExclusiveBlock FExclusiveBlock = new();

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
                FExclusiveBlock.Dispose();

            base.Dispose(disposeManaged);
        }

        protected override async ValueTask AsyncDispose()
        {
            await FExclusiveBlock.DisposeAsync();
            await base.AsyncDispose();
        }

        public PermanentServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        public PermanentServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        public PermanentServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {
            using (FExclusiveBlock.Enter())
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
                State |= ServiceEntryStates.Instantiated;

                return true;
            }
        }

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;

        public override Lifetime Lifetime { get; } = new PermanentLifetime();

        public override bool IsShared => true;
    }
}
#endif