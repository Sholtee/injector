/********************************************************************************
* PooledLifetime.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal sealed class PooledLifetime : Lifetime
    {
        private AbstractServiceEntry GetPoolService(PooledServiceEntry entry) => new SingletonServiceEntry
        (
            entry.Interface.IsGenericTypeDefinition
                ? typeof(IPool<>)
                : typeof(IPool<>).MakeGenericType(entry.Interface),
            entry.PoolName,
            entry.Interface.IsGenericTypeDefinition
                ? typeof(PoolService<>)
                : typeof(PoolService<>).MakeGenericType(entry.Interface),
            new { capacity = Capacity, name = entry.Name },
            false // IPool<> doesn't use any aspects
        );

        public PooledLifetime() : base(precedence: 20) { }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, bool supportAspects)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            PooledServiceEntry entry = new(iface, name, implementation, supportAspects);

            yield return GetPoolService(entry);
            yield return entry;
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, object explicitArgs, bool supportAspects)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            if (explicitArgs is null)
                throw new ArgumentNullException(nameof(explicitArgs));

            PooledServiceEntry entry = new(iface, name, implementation, explicitArgs, supportAspects);

            yield return GetPoolService(entry);
            yield return entry;
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Expression<FactoryDelegate> factory, bool supportAspects)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            PooledServiceEntry entry = new(iface, name, factory, supportAspects);

            yield return GetPoolService(entry);
            yield return entry;
        }

        public override int CompareTo(LifetimeBase other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            return other is PooledLifetime
                //
                // It breaks the contract how the IComparable interface is supposed to be implemented but
                // required since a pooled service cannot have pooled dependency (dependency would never 
                // get back to its parent pool)
                //

                ? -1
                : base.CompareTo(other);
        }

        public int Capacity { get; init; } = Environment.ProcessorCount;

        public override string ToString() => nameof(Pooled);

        public override LifetimeBase Using(object configuration)
        {
            if (configuration is not PoolConfig config)
                throw new ArgumentException(Resources.INVALID_CONFIG, nameof(configuration));

            return new PooledLifetime
            {
                Capacity = config.Capacity,
            };
        }
    }
}
