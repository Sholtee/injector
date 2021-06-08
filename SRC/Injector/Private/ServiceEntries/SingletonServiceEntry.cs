/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;
    using Properties;

    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        private readonly ConcurrentBag<IServiceReference> FInstances = new();

        private readonly ExclusiveBlock FExclusiveBlock = new();

        protected override void SaveReference(IServiceReference serviceReference) => FInstances.Add(serviceReference);

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

        public SingletonServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {
            using (FExclusiveBlock.Enter())
            {
                EnsureAppropriateReference(reference);

                //
                // Singleton bejegyzeshez mindig sajat injector van letrehozva a deklaralo kontenerbol
                //

                IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
                Ensure.AreEqual(relatedInjector.UnderlyingContainer.Parent, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

                //
                // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
                // beallitott erteket.
                //

                if (State.HasFlag(ServiceEntryStates.Built)) return false;

                //
                // Kulonben legyartjuk
                //

                base.SetInstance(reference);

                State |= ServiceEntryStates.Built;

                return true;
            }
        }

        public override Lifetime Lifetime { get; } = Lifetime.Singleton;

        public override bool IsShared => true;

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;
    }
}