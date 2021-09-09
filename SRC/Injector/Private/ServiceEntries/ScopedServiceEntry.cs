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
    using Interfaces;

    internal class ScopedServiceEntry : ProducibleServiceEntry
    {
        private readonly IServiceReference?[] FInstances = new IServiceReference?[1]; // max egy eleme lehet

        private ScopedServiceEntry(ScopedServiceEntry entry, IServiceRegistry? owner) : base(entry, owner)
        {
        }

        protected override void SaveReference(IServiceReference serviceReference)
        {
            Debug.Assert(FInstances[0] is null, "Instance has already been set.");
            FInstances[0] = serviceReference;
        }

        public ScopedServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceRegistry? owner) : base(@interface, name, factory, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation, IServiceRegistry? owner) : base(@interface, name, implementation, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceRegistry? owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {
            CheckNotDisposed();
            EnsureAppropriateReference(reference);

            //
            // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a korabban 
            // beallitott erteket -> Minden egyes scope maximum egy sajat peldannyal rendelkezhet.
            //

            if (State.HasFlag(ServiceEntryStates.Built))
                return false;

            //
            // Kulomben legyartjuk
            //

            base.SetInstance(reference);

            State |= ServiceEntryStates.Built;

            return true;
        }

        public override AbstractServiceEntry CopyTo(IServiceRegistry registry) => new ScopedServiceEntry(this, Ensure.Parameter.IsNotNull(registry, nameof(registry)));

        public override AbstractServiceEntry Specialize(IServiceRegistry? owner, params Type[] genericArguments)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new ScopedServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    owner
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new ScopedServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    owner
                ),
                _ when Factory is not null => new ScopedServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    owner
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override Lifetime Lifetime { get; } = Lifetime.Scoped;

        public override IReadOnlyList<IServiceReference> Instances => (FInstances[0] is not null ? FInstances : Array.Empty<IServiceReference>())!;
    }
}