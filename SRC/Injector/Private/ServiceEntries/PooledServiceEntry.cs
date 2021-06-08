/********************************************************************************
* PooledServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;
    using Properties;

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

            if (State.HasFlag(ServiceEntryStates.Built)) return false;

            //
            // Pool-ban az eredeti factory-t hivjuk
            //

            if (relatedInjector.GetOption<bool>(PooledLifetime.POOL_SCOPE))
            {
                base.SetInstance(reference);
            }

            //
            // Fogyasztoban megszolitjuk a bejegyzeshez tartozo PoolService-t
            //

            else
            {
                IPool relatedPool = (IPool) relatedInjector.Get(typeof(IPool<>).MakeGenericType(Interface), PooledLifetime.GetPoolName(Interface, Name));

                //
                // Mivel a mogottes ObjectPool<>.Get() ugyanazt az entitast adja vissza ha ugyanabbol a szalbol tobbszor
                // hivjuk (es a szerviz maga hiaba Scoped ez siman lehetseges ha tobb injector-t hozunk letre ugyanabban
                // a szalban). Ezert h a felszabaditassal ne legyen kavarodas ilyen esetben kivetelt dobunk.
                //

                if (relatedPool.Any(item => item.OwnerThread == Thread.CurrentThread.ManagedThreadId))
                    throw new RequestNotAllowedException(Resources.POOL_ITEM_ALREADY_TAKEN);

                PoolItem<IServiceReference> poolItem = relatedPool.Get(CheckoutPolicy.Block)!; // CheckoutPolicy.Block miatt sose NULL

                //
                // Mivel a pool elem scope-ja kulonbozik "relatedInjector" scope-jatol (egymastol fuggetlenul 
                // felszabaditasra kerulhetnek) ezert felvesszuk az elemet fuggosegkent is h biztosan ne
                // legyen gond az elettartammal.
                //

                relatedInjector.Get<IServicePath>().Requestor?.AddDependency(poolItem.Value);

                //
                // Nem gond h a poolItem-et adjuk vissza, igy NEM annak tartalma kerul felszabaditasra a 
                // scope lezarasakor (poolItem felszabaditasa visszateszi a legyartott elemet a pool-ba).
                //

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

            if (!reference.RelatedInjector!.GetOption<bool>(PooledLifetime.POOL_SCOPE))
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