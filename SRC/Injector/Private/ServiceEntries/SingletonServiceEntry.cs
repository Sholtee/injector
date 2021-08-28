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

    internal class SingletonServiceEntry : ProducibleServiceEntrySupportsProxying
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

        private SingletonServiceEntry(SingletonServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
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
            CheckNotDisposed();

            using (FExclusiveBlock.Enter())
            {
                EnsureAppropriateReference(reference);

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

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    Owner
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Owner
                ),
                _ when Factory is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    Owner
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override AbstractServiceEntry Copy() => new SingletonServiceEntry(this, null!);

        public override Lifetime Lifetime { get; } = Lifetime.Singleton;

        public override bool IsShared { get; } = true;

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;
    }
}