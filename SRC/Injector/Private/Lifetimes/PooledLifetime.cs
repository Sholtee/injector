/********************************************************************************
* PooledLifetime.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed partial class PooledLifetime : Lifetime, IHasCapacity
    {
        private IEnumerable<AbstractServiceEntry> Register(PooledServiceEntry entry)
        {
            yield return entry;

            //
            // PoolItem<object> altal megvalositott interface-eket nem regisztralhatjuk (h
            // a konkret oljektum helyett ne a PoolItem-et adja vissza az injector).
            //

            if (typeof(PoolItem<>).MakeGenericType(entry.Interface).GetInterfaces().Contains(entry.Interface))
                throw new NotSupportedException();

            //
            // Letrehozunk egy dedikaltan ehhez a bejegyzeshez tartozo pool-szervizt (ha nem generikusunk van).
            //

            string factoryName = $"__factory_{entry.FriendlyName()}";

            //
            // Ez minden egyes pool bejegyzeshez sajat scope-ot hoz letre.
            //

            if (entry.Implementation is not null)
                yield return new PermanentServiceEntry(entry.Interface, factoryName, entry.Implementation, entry.Owner);
            else if (entry.Factory is not null)
                yield return new PermanentServiceEntry(entry.Interface, factoryName, entry.Factory, entry.Owner);
            else
                throw new NotSupportedException();

            //
            // Ez magat a pool szervizt hozza letre amit kesobbb a PooledServiceEntry.SetInstance() metodusaban
            // szolitunk meg.
            //

            yield return new SingletonServiceEntry
            (
                typeof(IPool<>).MakeGenericType(entry.Interface),
                entry.Name,
                typeof(PoolService<>).MakeGenericType(entry.Interface),
                new Dictionary<string, object?>
                {
                    //
                    // Az argumentum nevek meg kell egyezzenek a PoolService.ctor() parameter neveivel.
                    // Kesobb majd lehet szebben is megoldhato lesz: https://github.com/dotnet/csharplang/issues/373
                    //

                    { "capacity", Capacity },
                    { "factoryName", factoryName }
                },
                entry.Owner
            );
        }

        [ModuleInitializer]
        public static void Setup() => Pooled = new PooledLifetime();

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => Register
        (
             new PooledServiceEntry(iface, name, implementation, owner, this)
        );

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) => Register
        (
            new PooledServiceEntry(iface, name, implementation, explicitArgs, owner, this)
        );

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => Register
        (
            new PooledServiceEntry(iface, name, factory, owner, this)
        );

        public override bool IsCompatible(AbstractServiceEntry entry) => entry is PooledServiceEntry;

        public override string ToString() => nameof(Pooled);

        public override object Clone() => new PooledLifetime
        {
            Capacity = Capacity
        };

        public int Capacity { get; set; } = Environment.ProcessorCount;
    }
}
