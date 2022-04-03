/********************************************************************************
* PooledLifetime.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class PooledLifetime : InjectorDotNetLifetime, IHasCapacity
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
            new { capacity = Capacity, name },
            null
        );
 
        private static string GetPoolName(Type iface, string? name)
        {
            if (iface.IsConstructedGenericType)
                iface = iface.GetGenericTypeDefinition();

            return $"{Consts.INTERNAL_SERVICE_NAME_PREFIX}pool_{(iface, name).GetHashCode():X}";
        }

        public PooledLifetime() : base(precedence: 20) => Pooled = this;

        public const string POOL_SCOPE = nameof(POOL_SCOPE); // TODO: Remove

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation)
        {
            string poolName = GetPoolName(iface, name);

            //
            // A sorrend szamit (last IModifiedServiceCollection.LastEntry) viszont a validalas miatt eloszor a 
            // PooledServiceEntry-t hozzuk letre.
            //

            PooledServiceEntry pooledServiceEntry = new(iface, name, implementation, null, poolName);

            yield return GetPoolService(iface, name, poolName);
            yield return pooledServiceEntry;
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, object explicitArgs)
        {
            string poolName = GetPoolName(iface, name);

            PooledServiceEntry pooledServiceEntry = new(iface, name, implementation, explicitArgs, null, poolName);

            yield return GetPoolService(iface, name, poolName);
            yield return pooledServiceEntry;
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory)
        {
            string poolName = GetPoolName(iface, name);

            PooledServiceEntry pooledServiceEntry = new(iface, name, factory, null, poolName);

            yield return GetPoolService(iface, name, poolName);
            yield return pooledServiceEntry;
        }

        public override int CompareTo(Lifetime other) => other is PooledLifetime
            //
            // Ez itt bar megtori a szabalyt amikent az IComparable-t implementalni kene meg is szukseges
            // mivel pooled szerviznek nem lehet pooled fuggosege (a fuggoseg sosem kerulne vissza a szulo
            // pool-ba)
            //

            ? -1
            : base.CompareTo(other);

        public override object Clone() => new PooledLifetime
        {
            Capacity = Capacity
        };

        public int Capacity { get; set; } = Environment.ProcessorCount;

        public override string ToString() => nameof(Pooled);
    }
}
