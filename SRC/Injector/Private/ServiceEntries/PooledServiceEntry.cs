/********************************************************************************
* PooledServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    //
    // A PooledServiceEntry ket modon is lehet peldanyositva: Egy kulonallo poolban vagy a felhasznalo oldalan.
    //

    internal class PooledServiceEntry : ProducibleServiceEntry
    {
        private readonly IServiceReference?[] FInstances = new IServiceReference?[1]; // max egy eleme lehet

        protected override void SaveReference(IServiceReference serviceReference) => FInstances[0] = serviceReference;

        protected PooledServiceEntry(PooledServiceEntry entry, IServiceRegistry? owner) : base(entry, owner)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceRegistry? owner) : base(@interface, name, factory, owner)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IServiceRegistry? owner) : base(@interface, name, implementation, owner)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceRegistry? owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {
            CheckNotDisposed();
            EnsureAppropriateReference(reference);

            IInjector relatedInjector = reference.Scope!;

            //
            // Ha mar le lett gyartva akkor nincs dolgunk, jelezzuk a hivonak h ovlassa ki a
            // korabban beallitott erteket -> Kovetkezes kepp egy scope MINDIG csak egy 
            // elemet vehet ki a pool-bol
            //

            if (State.HasFlag(ServiceEntryStates.Built))
                return false;

            //
            // Pool-ban az eredeti factory-t hivjuk
            //

            if (relatedInjector.Meta(PooledLifetime.POOL_SCOPE) is true)
            {
                base.SetInstance(reference);
            }

            //
            // Fogyasztoban megszolitjuk a bejegyzeshez tartozo PoolService-t
            //

            else
            {
                //
                // Mivel itt elkerjuk a pool-t magat ezert annak a felszabaditasa biztosan a mi felszabaditasunk utan
                // fog megtortenni: biztonsagosan visszahelyezhetjuk mindig az elkert szervizt a pool-ba
                //

                IPool relatedPool = (IPool) relatedInjector.Get(typeof(IPool<>).MakeGenericType(Interface), PooledLifetime.GetPoolName(Interface, Name));

                reference.Value = relatedPool.Get();
                SaveReference(reference);
            }

            State |= ServiceEntryStates.Built;

            return true;
        }

        public sealed override AbstractServiceEntry CopyTo(IServiceRegistry registry) => new PooledServiceEntry(this, Ensure.Parameter.IsNotNull(registry, nameof(registry)));

        public override AbstractServiceEntry Specialize(IServiceRegistry? owner, params Type[] genericArguments)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    owner
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    owner
                ),
                _ when Factory is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    owner
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override Lifetime Lifetime { get; } = Lifetime.Pooled;

        public override IReadOnlyList<IServiceReference> Instances => (FInstances[0] is not null ? FInstances : Array.Empty<IServiceReference>())!;
    }
}