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
    using Primitives.Threading;
    using Properties;

    //
    // A PooledServiceEntry ket modon is lehet peldanyositva: Egy kulonallo poolban vagy a felhasznalo oldalan.
    //

    internal class PooledServiceEntry : ProducibleServiceEntryBase, ISupportsSpecialization
    {
        private readonly List<IServiceReference> FInstances = new(1); // max egy eleme lehet

        protected override void SaveReference(IServiceReference serviceReference) => FInstances.Add(serviceReference);

        protected PooledServiceEntry(PooledServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {
            CheckNotDisposed();
            EnsureAppropriateReference(reference);

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

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

                PoolItem<IServiceReference> poolItem = relatedPool.Get();

                reference.Value = poolItem;
                SaveReference(reference);
            }

            State |= ServiceEntryStates.Built;

            return true;
        }

        public sealed override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(target, nameof(target));

            //
            // Ez itt trukkos mert a leszarmazottban is csak "sima" PooledServiceEntry-t regisztral
            // (igy az mar nem lesz proxy-zhato).
            //

            var result = new PooledServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        AbstractServiceEntry ISupportsSpecialization.Specialize(params Type[] genericArguments)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return this switch
            {
                //
                // Itt ne a "Lifetime"-ot hasznaljuk mert a pool-t nem szeretnenk megegyszer regisztralni.
                //

                _ when Implementation is not null && ExplicitArgs is null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    Owner
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Owner
                ),
                _ when Factory is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    Owner
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override object GetInstance(IServiceReference reference)
        {
            object instance = base.GetInstance(reference);

            if (reference.RelatedInjector!.Meta(PooledLifetime.POOL_SCOPE) is not true)
            {
                //
                // Ha fogyaszto oldalon vagyunk akkor PoolItem-et kapunk vissza, abbol kell elovarazsolni az erteket
                //

                var poolItem = (PoolItem<IServiceReference>) instance;
                return poolItem
                    .Value
                    .GetInstance();
            }

            //
            // Pool scope-ban nincs dolgunk
            //

            return instance;
        }

        public override Lifetime Lifetime { get; } = Lifetime.Pooled;

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;
    }
}