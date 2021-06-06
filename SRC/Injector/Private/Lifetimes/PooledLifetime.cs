/********************************************************************************
* PooledLifetime.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed partial class PooledLifetime : InjectorDotNetLifetime<PooledLifetime>, IHasCapacity
    {
        [SuppressMessage("Performance", "CA1802:Use literals where appropriate", Justification = "Due to interpolation it cannot be const.")]
        public static readonly string POOL_SCOPE = $"_{nameof(POOL_SCOPE)}";

        public PooledLifetime() : base(bindTo: () => Pooled, precedence: 20) { }

        [ModuleInitializer]
        public static void Setup() => Bind();

        public static string GetPoolName(Type iface, string? name) =>
            //
            // Type.GUID lezart es nyilt generikusnal is azonos
            //

            $"{ServiceContainer.INTERNAL_SERVICE_NAME_PREFIX}pool_{iface.GUID}_{name}";

        //
        // Nem kell a kulonbozo parametereket validalni. Ha vmelyikkel gaz van akkor ide mar el sem
        // jutunk.
        //

        private AbstractServiceEntry GetPoolEntry(Type iface, string? name, IServiceContainer owner) => new SingletonServiceEntry
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
            owner
        );

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner)
        {
            yield return new PooledServiceEntrySupportsProxying(iface, name, implementation, owner);
            yield return GetPoolEntry(iface, name, owner);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner)
        {
            yield return new PooledServiceEntrySupportsProxying(iface, name, implementation, explicitArgs, owner);
            yield return GetPoolEntry(iface, name, owner);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner)
        {
            yield return new PooledServiceEntrySupportsProxying(iface, name, factory, owner);
            yield return GetPoolEntry(iface, name, owner);
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
