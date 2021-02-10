/********************************************************************************
* PooledLifetime.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed partial class PooledLifetime : Lifetime, IHasCapacity
    {
        private IEnumerable<AbstractServiceEntry> CreateEntries(Type iface, string? name, object implOrFactory, IReadOnlyDictionary<string, object>? explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters)
        {
            //
            // Type.GUID lezart es nyilt generikusnal is azonos
            //

            string
                poolName = $"{ServiceContainer.INTERNAL_SERVICE_NAME_PREFIX}pool_{iface.GUID}_{name}",
                factoryName = $"{ServiceContainer.INTERNAL_SERVICE_NAME_PREFIX}factory_{iface.GUID}_{name}";

            //
            // Pool megszolitasat vegzo szerviz.
            //

            yield return new ScopedServiceEntry 
            (
                iface, // meg lehet generikus
                name, 
                GetFromPool,
                owner,
                new Func<object, Type, object>[] { GetFromPoolItem }.Concat(customConverters).ToArray()
            );

            object GetFromPool(IInjector injector, Type concreteIface) 
            {
                //
                // A szervizhez tartozo pool lekerdezese. Generikus esetben "concreteIface" itt mar biztosan lezart.
                //

                Debug.Assert(!concreteIface.IsGenericTypeDefinition);

                IPool relatedPool = (IPool) injector.Get(typeof(IPool<>).MakeGenericType(concreteIface), poolName);

                PoolItem<IServiceReference> poolItem = relatedPool.Get(CheckoutPolicy.Block);

                //
                // Mivel a pool elem scope-ja kulonbozik "injector" scope-jatol (egymastol fuggetlenul 
                // felszabaditasra kerulhetnek) ezert felvesszuk az elemet fuggosegkent is h biztosan
                // ne legyen gond az elettartammal.
                //

                injector.Get<IServiceGraph>().Requestor?.AddDependency(poolItem.Value);

                //
                // Nem gond h a poolItem-et adjuk vissza, igy nem annak tartalma kerul felszabaditasra a 
                // scope lezarasakor.
                //

                return poolItem;
            }

            //
            // Pool elemek legyartasat vegzo szerviz (minden egyes legyartott bejegyzes sajat scope-al rendelkezik,
            // elettartamukat "owner" kezeli).
            //

            yield return implOrFactory switch
            {
                Type implementation when explicitArgs is null => 
                    new ScopedServiceEntry(iface, factoryName, implementation, owner),
                Type implementation when explicitArgs is not null => 
                    new ScopedServiceEntry(iface, factoryName, implementation, explicitArgs!, owner),
                Func<IInjector, Type, object> factory => 
                    new ScopedServiceEntry(iface, factoryName, factory, owner),
                _ => 
                    throw new NotSupportedException()
            };

            //
            // Pool szerviz maga.
            //

            yield return new SingletonServiceEntry
            (
                iface.IsGenericTypeDefinition
                    ? typeof(IPool<>) 
                    : typeof(IPool<>).MakeGenericType(iface),
                poolName,
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
                    ["factoryName"] = factoryName
                },
                owner
            );
        }

        private static object GetFromPoolItem(object poolItem, Type iface) => ((PoolItem<IServiceReference>) poolItem).Value.Value!;

        [ModuleInitializer]
        public static void Setup() => Pooled = new PooledLifetime();

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters) => 
            CreateEntries(iface, name, implementation, null, owner, customConverters);

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters) =>
            CreateEntries(iface, name, implementation, explicitArgs!, owner, customConverters);

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters) =>
            CreateEntries(iface, name, factory, null, owner, customConverters);

        public override bool IsCompatible(AbstractServiceEntry entry) => entry.CustomConverters.Contains(GetFromPoolItem);

        public override string ToString() => nameof(Pooled);

        public override object Clone() => new PooledLifetime
        {
            Capacity = Capacity
        };

        public int Capacity { get; set; } = Environment.ProcessorCount;
    }
}
