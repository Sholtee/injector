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

    internal sealed class PooledLifetime : InjectorDotNetLifetime, IHasCapacity, IConcreteLifetime<PooledLifetime>
    {
        public PooledLifetime() : base(bindTo: () => Pooled, precedence: 20) { }

        public const string POOL_SCOPE = nameof(POOL_SCOPE);

        public static string GetPoolName(Type iface, string? name)
        {
            if (iface.IsGenericType)
                iface = iface.GetGenericTypeDefinition();

            return $"{Consts.INTERNAL_SERVICE_NAME_PREFIX}pool_{iface.FullName}_{name}"; // ne iface.GUID-ot hasznaljunk mert az kibaszott lassu
        }

        private AbstractServiceEntry PoolService(Type iface, string? name)
        {
            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));

            return new SingletonServiceEntry
            (
                iface.IsGenericTypeDefinition
                    ? typeof(IPool<>)
                    : typeof(IPool<>).MakeGenericType(iface),
                GetPoolName(iface, name),
                iface.IsGenericTypeDefinition
                    ? typeof(PoolService<>)
                    : typeof(PoolService<>).MakeGenericType(iface),
                new Dictionary<string, object?>
                {
                    //
                    // Az argumentum nevek meg kell egyezzenek a PoolService.ctor() parameter neveivel.
                    // Kesobb majd lehet szebben is megoldhato lesz: https://github.com/dotnet/csharplang/issues/373
                    //

                    ["capacity"] = Capacity,
                    ["name"] = name
                },
                null
            );
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation)
        {
            //
            // A sorrent szamit (last IModifiedServiceCollection.LastEntry)
            //

            yield return PoolService(iface, name);
            yield return new PooledServiceEntry(iface, name, implementation, null);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs)
        {
            yield return PoolService(iface, name);
            yield return new PooledServiceEntry(iface, name, implementation, explicitArgs, null);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory)
        {
            yield return PoolService(iface, name);
            yield return new PooledServiceEntry(iface, name, factory, null);
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
    }
}
