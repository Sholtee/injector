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
        private static object GetPoolName(Type type, object? key)
        {
            if (type.IsConstructedGenericType)
                type = type.GetGenericTypeDefinition();

            return new
            {
                __pool_interface = type,
                __pool_key = key
            };
        }

        private AbstractServiceEntry GetPoolService(PooledServiceEntry entry) => new SingletonServiceEntry
        (
            entry.Type.IsGenericTypeDefinition
                ? typeof(IPool<>)
                : typeof(IPool<>).MakeGenericType(entry.Type),
            entry.PoolName,
            entry.Type.IsGenericTypeDefinition
                ? typeof(PoolService<>)
                : typeof(PoolService<>).MakeGenericType(entry.Type),
            new { config = Config, key = entry.Key },
            ServiceOptions.Default with { SupportAspects = false }
        );

        public const string POOL_SCOPE = nameof(POOL_SCOPE);

        public PooledLifetime() : base(precedence: 20) { }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, Type implementation, ServiceOptions serviceOptions)
        {
            PooledServiceEntry entry = new
            (
                type ?? throw new ArgumentNullException(nameof(type)),
                key,
                implementation ?? throw new ArgumentNullException(nameof(implementation)),
                serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions)),
                GetPoolName(type, key)
            );

            yield return GetPoolService(entry);
            yield return entry;
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, Type implementation, object explicitArgs, ServiceOptions serviceOptions)
        {
            PooledServiceEntry entry = new
            (
                type ?? throw new ArgumentNullException(nameof(type)),
                key,
                implementation ?? throw new ArgumentNullException(nameof(implementation)),
                explicitArgs ?? throw new ArgumentNullException(nameof(explicitArgs)),
                serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions)),
                GetPoolName(type, key)
            );

            yield return GetPoolService(entry);
            yield return entry;
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, Expression<FactoryDelegate> factory, ServiceOptions serviceOptions)
        {
            PooledServiceEntry entry = new
            (
                type ?? throw new ArgumentNullException(nameof(type)),
                key,
                factory ?? throw new ArgumentNullException(nameof(factory)),
                serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions)),
                GetPoolName(type, key)
            );

            yield return GetPoolService(entry);
            yield return entry;
        }

        public override int CompareTo(LifetimeBase other) => other is PooledLifetime
            //
            // It breaks the contract how the IComparable interface is supposed to be implemented but
            // required since a pooled service cannot have pooled dependency (dependency would never 
            // get back to its parent pool)
            //

            ? -1
            : base.CompareTo(other ?? throw new ArgumentNullException(nameof(other)));

        public PoolConfig Config { get; init; } = PoolConfig.Default;

        public override string ToString() => nameof(Pooled);

        public override LifetimeBase Using(object configuration) => new PooledLifetime
        {
            Config = configuration as PoolConfig ?? throw new ArgumentException(Resources.INVALID_CONFIG, nameof(configuration))
        };
    }
}
