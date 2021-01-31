/********************************************************************************
* PooledServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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

        private string PoolName { get; } = $"__Pool_{Interlocked.Increment(ref LUID)}";

        private PooledServiceEntry(PooledServiceEntry entry, IServiceContainer owner) : base(entry, owner) =>
            //
            // Leszarmazott kontenerben mar nem baszogathatjuk a factory-t.
            //

            Factory = null;
        #endregion

        public PooledServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner) => owner.Add
        (
            //
            // Letrehozunk egy dedikaltan ehhez a bejegyzeshez tartozo pool-szervizt.
            //

            new SingletonServiceEntry
            (
                typeof(IPool), 
                PoolName, 
                (injector, iface) => new UnderlyingPool(Capacity, () => Factory!.Invoke(injector, iface)), 
                owner
            )
        );

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
            //
            // Itt nem kell letrehozni semmit, mert lehet generikus szervizt regisztralunk
            //
        }

        public int Capacity { get; set; } = Environment.ProcessorCount;

        public override bool SetInstance(IServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            EnsureAppropriateReference(reference);

            Ensure.AreEqual(reference.RelatedInjector?.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

            IPool relatedPool = reference.RelatedInjector!.Get<IPool>(PoolName);

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

        public override Lifetime Lifetime { get; } = Lifetime.Singleton;
    }
}