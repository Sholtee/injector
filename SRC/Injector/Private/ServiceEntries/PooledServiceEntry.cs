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
    using Primitives.Patterns;
    using Properties;

    /// <summary>
    /// Describes a pooled service entry.
    /// </summary>
    internal class PooledServiceEntry : ProducibleServiceEntry
    {
        #region Private
        private interface IPool 
        {
            PoolItem<object> Get(CheckoutPolicy checkoutPolicy);
        }

        private sealed class UnderlyingPool : ObjectPool<object>, IPool
        {
            public UnderlyingPool(int maxPoolSize, Func<object> factory) : base(maxPoolSize, factory)
            {
            }

            public PoolItem<object> Get(CheckoutPolicy checkoutPolicy) => Get(checkoutPolicy, default);
        }

        private static int LUID;

        private string PoolName { get; set; }

        private PooledServiceEntry(PooledServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
            //
            // Leszarmazott kontenerben mar nem baszogathatjuk a factory-t es a pool is mar regisztralva van
            //

            Factory = null;
            PoolName = entry.PoolName;
        }

        private void RegisterPool(int capacity)
        {
            //
            // PoolItem<object> altal megvalositott interface-eket nem regisztralhatjuk (h
            // a konkret oljektum helyett ne a PoolItem-et adja vissza az injector).
            //

            if (typeof(PoolItem<object>).GetInterfaces().Contains(Interface))
                throw new NotSupportedException();

            //
            // Letrehozunk egy dedikaltan ehhez a bejegyzeshez tartozo pool-szervizt (ha nem generikusunk van).
            //

            if (Factory is not null)
            {
                PoolName = $"__Pool_{Interlocked.Increment(ref LUID)}";

                string factoryName = $"{PoolName}_Factory";

                Owner.Add
                (
                    //
                    // Ez minden egyes pool bejegyzeshez sajat scope-ot hoz letre
                    //

                    new PermanentServiceEntry
                    (
                        Interface,
                        factoryName,
                        (injector, _) => Factory.Invoke(injector, Interface),
                        Owner
                    )
                );

                Owner.Add
                (
                    new SingletonServiceEntry
                    (
                        typeof(IPool),
                        PoolName,
                        (injector, _) => new UnderlyingPool(capacity, () =>
                        {
                            //
                            // Ez itt trukkos mert:
                            // 1) "injector" by design nem szalbiztos viszont ez a metodus lehet hivva paralell
                            // 2) Minden egyes legyartott elemnek sajat scope kell (h ok maguk szalbiztosak legyenek)
                            // 3) Letrehozaskor a mar meglevo grafot boviteni kell 
                            //

                            lock (injector) // maximum csak "capacity"-szer lesz hivva -> a lock erdemben nem befolyasolja a teljesitmenyt
                            {
                                return injector.Get(Interface, factoryName);
                            }
                        }),
                        Owner
                    )
                );
            }
        }
        #endregion

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public PooledServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, int capacity) : base(@interface, name, factory, owner) =>
            RegisterPool(capacity);

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner, int capacity) : base(@interface, name, implementation, owner) =>
            RegisterPool(capacity);
        #pragma warning restore CS8618

        public override bool SetInstance(IServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            EnsureAppropriateReference(reference);

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

            IPool relatedPool = relatedInjector.Get<IPool>(PoolName);

            //
            // Nem gond h PoolItem-et adunk a referencia ertekeul, mivel az implementalja ICustomAdapter-t
            // amit az injector faszan lekezel.
            //

            reference.Value = relatedPool.Get(CheckoutPolicy.Block);

            Instances = new[] { reference };

            return Built = true;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(target, nameof(target));

            var result = new PooledServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        public override Lifetime Lifetime { get; } = Lifetime.Pooled;
    }
}