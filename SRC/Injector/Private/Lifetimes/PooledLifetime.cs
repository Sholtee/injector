/********************************************************************************
* PooledLifetime.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed partial class PooledLifetime : Lifetime, IHasCapacity
    {
        private IEnumerable<AbstractServiceEntry> CreateEntries(Type iface, string? name, object implOrFactory, IReadOnlyDictionary<string, object>? explicitArgs, IServiceContainer owner)
        {
            //
            // Pool megszolitasat vegzo szerviz.
            //

            ScopedServiceEntry accessor = new 
            (
                iface, 
                name, 
                GetFromPool, 
                owner,
                GetFromPoolItem
            );
            yield return accessor;

            object GetFromPool(IInjector injector, Type iface) 
            {
                //
                // A szervizhet tartozo pool lekerdezese
                //

                IPool relatedPool = (IPool) injector.Get(typeof(IPool<>).MakeGenericType(iface), name);

                //
                // Nem gond h PoolItem-et adunk a vissza, ezt a CustomCoverter lekezeli.
                //

                return relatedPool.Get(CheckoutPolicy.Block);
            }

            string factoryName = $"__factory_{accessor.FriendlyName()}";

            //
            // Pool elemek legyartasat vegzo szerviz (minden egyes legyartott bejegyzes sajat scope-al rendelkezik,
            // elettartamukat "owner" kezeli).
            //

            yield return implOrFactory switch
            {
                Type implementation when explicitArgs is null => new PermanentServiceEntry(iface, factoryName, implementation, owner),
                Type implementation when explicitArgs is not null => new PermanentServiceEntry(iface, factoryName, implementation, explicitArgs!, owner),
                Func<IInjector, Type, object> factory => new PermanentServiceEntry(iface, factoryName, factory, owner),
                _ => throw new NotSupportedException()
            };

            //
            // Pool szerviz maga.
            //

            yield return new SingletonServiceEntry
            (
                typeof(IPool<>).MakeGenericType(iface),
                name,
                typeof(PoolService<>).MakeGenericType(iface),
                new Dictionary<string, object?>
                {
                    //
                    // Az argumentum nevek meg kell egyezzenek a PoolService.ctor() parameter neveivel.
                    // Kesobb majd lehet szebben is megoldhato lesz: https://github.com/dotnet/csharplang/issues/373
                    //

                    { "capacity", Capacity },
                    { "factoryName", factoryName }
                },
                owner
            );
        }

        private static object GetFromPoolItem(object poolItem) =>
            #pragma warning disable 0618
            ((ICustomAdapter) poolItem).GetUnderlyingObject();
            #pragma warning restore 0618

        [ModuleInitializer]
        public static void Setup() => Pooled = new PooledLifetime();

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => 
            CreateEntries(iface, name, implementation, null, owner);

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) =>
            CreateEntries(iface, name, implementation, explicitArgs!, owner);

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) =>
            CreateEntries(iface, name, factory, null, owner);

        public override bool IsCompatible(AbstractServiceEntry entry) => entry.CustomConverters.Contains(GetFromPoolItem);

        public override string ToString() => nameof(Pooled);

        public override object Clone() => new PooledLifetime
        {
            Capacity = Capacity
        };

        public int Capacity { get; set; } = Environment.ProcessorCount;
    }
}
