/********************************************************************************
* PooledServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
        private readonly List<IServiceReference> FInstances = new List<IServiceReference>(1); // max egy lehet

        private PooledServiceEntry(PooledServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
            //
            // Leszarmazott kontenerben mar nem baszogathatjuk a factory-t es a pool is mar regisztralva van
            //

            Factory = null;
            Lifetime = entry.Lifetime;
        }

        //
        // Lifetime-ot csak azert adjuk at h lekerdezhessuk a kapacitast
        //

        public PooledServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, PooledLifetime lifetime) : base(@interface, name, factory, owner) 
        {
            Lifetime = lifetime;
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner, PooledLifetime lifetime) : base(@interface, name, implementation, owner) 
        {
            Lifetime = lifetime;
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, PooledLifetime lifetime) : base(@interface, name, implementation, explicitArgs, owner)
        {
            Lifetime = lifetime;
        }

        public override bool SetInstance(IServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            EnsureAppropriateReference(reference);

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

            //
            // A szervizhet tartozo pool lekerdezese
            //

            IPool relatedPool = (IPool) relatedInjector.Get(typeof(IPool<>).MakeGenericType(Interface), Name);

            //
            // Nem gond h PoolItem-et adunk a referencia ertekeul, mivel az implementalja ICustomAdapter-t
            // amit az injector faszan lekezel.
            //

            reference.Value = relatedPool.Get(CheckoutPolicy.Block);
            FInstances.Add(reference);

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

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;

        public override IReadOnlyCollection<Func<object, object>> CustomConverters { get; } = new Func<object, object>[]
        {
            #pragma warning disable 0618
            poolItem => ((ICustomAdapter) poolItem).GetUnderlyingObject()
            #pragma warning restore 0618
        };

        public override Lifetime Lifetime { get; }
    }
}