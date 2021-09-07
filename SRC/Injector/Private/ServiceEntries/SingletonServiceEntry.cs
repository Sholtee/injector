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

    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        private readonly ConcurrentBag<IServiceReference> FInstances = new();

        private readonly ExclusiveBlock FExclusiveBlock = new(ExclusiveBlockFeatures.SupportsRecursion); // rekurzio tamogatas kell h a CDEP detektalas mukodjon

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

        private SingletonServiceEntry(SingletonServiceEntry entry, IServiceRegistry? owner) : base(entry, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceRegistry? owner) : base(@interface, name, factory, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, IServiceRegistry? owner) : base(@interface, name, implementation, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceRegistry? owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {
            CheckNotDisposed();
            EnsureAppropriateReference(reference);

            using (FExclusiveBlock.Enter())
            {
                //
                // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
                // beallitott erteket.
                //

                if (State.HasFlag(ServiceEntryStates.Built))
                    return false;

                //
                // Kulonben legyartjuk
                //

                base.SetInstance(reference);

                State |= ServiceEntryStates.Built;

                return true;
            }
        }

        public override AbstractServiceEntry Specialize(IServiceRegistry? owner, params Type[] genericArguments) // TODO: torolni
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
                    owner
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    owner
                ),
                _ when Factory is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    owner
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override AbstractServiceEntry CopyTo(IServiceRegistry registry) => new SingletonServiceEntry(this, Ensure.Parameter.IsNotNull(registry, nameof(registry)));

        public override Lifetime Lifetime { get; } = Lifetime.Singleton;

        public override bool IsShared { get; } = true;

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;
    }
}