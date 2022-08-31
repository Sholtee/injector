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

    internal sealed class PooledLifetime : InjectorDotNetLifetime
    {
        private AbstractServiceEntry GetPoolService(Type iface, string? name, string poolName) => new SingletonServiceEntry
        (
            iface.IsGenericTypeDefinition
                ? typeof(IPool<>)
                : typeof(IPool<>).MakeGenericType(iface),
            poolName,
            iface.IsGenericTypeDefinition
                ? typeof(PoolService<>)
                : typeof(PoolService<>).MakeGenericType(iface),
            new { capacity = Capacity, name }
        );

        private static string GetPoolName(Type iface, string? name)
        {
            if (iface.IsConstructedGenericType)
                iface = iface.GetGenericTypeDefinition();

            return $"{Consts.INTERNAL_SERVICE_NAME_PREFIX}pool_{(iface, name).GetHashCode():X}";
        }

        public PooledLifetime() : base(precedence: 20) => Pooled = this;

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            string poolName = GetPoolName(iface, name);

            //
            // PooledServiceEntry is created first (to do the neccessary validations) but returned last
            // (thus the IModifiedServiceCollection.LastEntry won't be screwed up)
            //

            PooledServiceEntry entry = new(iface, name, implementation, poolName);

            yield return GetPoolService(iface, name, poolName);
            yield return entry;
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, object explicitArgs)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            if (explicitArgs is null)
                throw new ArgumentNullException(nameof(explicitArgs));

            string poolName = GetPoolName(iface, name);

            PooledServiceEntry entry = new(iface, name, implementation, explicitArgs, poolName);

            yield return GetPoolService(iface, name, poolName);
            yield return entry;
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Expression<Func<IInjector, Type, object>> factory)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            string poolName = GetPoolName(iface, name);

            PooledServiceEntry entry = new(iface, name, factory, poolName);

            yield return GetPoolService(iface, name, poolName);
            yield return entry;
        }

        public override int CompareTo(Lifetime other)
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

        public override Lifetime Using(object configuration)
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            return new PooledLifetime
            {
                Capacity = ((PoolConfig) configuration).Capacity,
            };
        }
    }
}
