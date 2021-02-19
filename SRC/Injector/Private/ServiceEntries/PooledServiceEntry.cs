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

    /// <summary>
    /// Describes a pooled service entry.
    /// </summary>
    internal class PooledServiceEntry : ProducibleServiceEntryBase, ISupportsSpecialization
    {
        private readonly List<IServiceReference> FInstances = new List<IServiceReference>(1); // max egy eleme lehet

        protected PooledServiceEntry(PooledServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, factory, owner, customConverters)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, owner, customConverters)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, explicitArgs, owner, customConverters)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {       
            EnsureAppropriateReference(reference);
            EnsureProducible();

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

            if (relatedInjector.Get<IReadOnlyDictionary<string, object>>($"{ServiceContainer.INTERNAL_SERVICE_NAME_PREFIX}options").GetValueOrDefault<bool>(PooledLifetime.POOL_SCOPE))
            {
                reference.Value = Factory!(relatedInjector, Interface);
            }

            //
            // Kulonben megszolitjuk a bejegyzeshez tartozo PoolService-t
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

                relatedInjector.Get<IServiceGraph>().Requestor?.AddDependency(poolItem.Value);

                //
                // Nem gond h a poolItem-et adjuk vissza, igy NEM annak tartalma kerul felszabaditasra a 
                // scope lezarasakor (poolItem felszabaditasa visszateszi a legyartott elemet a pool-ba).
                //

                reference.Value = poolItem;
            }

            FInstances.Add(reference);
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

        AbstractServiceEntry ISupportsSpecialization.Specialize(params Type[] genericArguments) => this switch
        {
            //
            // Itt ne a "Lifetime"-ot hasznaljuk mert a pool-t nem szeretnenk megegyszer regisztralni.
            //

            _ when Implementation is not null && ExplicitArgs is null => new PooledServiceEntry
            (
                Interface.MakeGenericType(genericArguments),
                Name,
                Implementation.MakeGenericType(genericArguments),
                Owner,
                CustomConverters.ToArray()
            ),
            _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntry
            (
                Interface.MakeGenericType(genericArguments),
                Name,
                Implementation.MakeGenericType(genericArguments),
                ExplicitArgs,
                Owner,
                CustomConverters.ToArray()
            ),
            _ when Factory is not null => new PooledServiceEntry
            (
                Interface.MakeGenericType(genericArguments),
                Name,
                Factory,
                Owner,
                CustomConverters.ToArray()
            ),
            _ => throw new NotSupportedException()
        };

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;

        public override Lifetime Lifetime { get; } = Lifetime.Pooled;
    }
}