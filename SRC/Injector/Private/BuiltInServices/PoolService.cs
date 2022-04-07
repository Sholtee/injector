/********************************************************************************
* PoolService.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Primitives.Threading;

    //                                        !!!FIGYELEM!!!
    //
    // Ez az osztaly kozponti komponens, ezert minden modositast korultekintoen, a teljesitmenyt szem elott tartva
    // kell elvegezni:
    // - nincs Sysmte.Linq
    // - nincs System.Reflection
    // - mindig futtassuk a teljesitmeny teszteket (is) hogy a hatekonysag nem romlott e
    //

    internal sealed class PoolService<TInterface> : ObjectPool<object>, IPool<TInterface> where TInterface: class
    {
        private sealed class PoolServiceLifetimeManager: Disposable, ILifetimeManager<object>
        {
            //
            // Ne [ThreadStatic]-t hasznaljunk, hogy minden interface-nev paroshoz kulon peldany tartozzon.
            //

            private readonly ThreadLocal<IInjector> FDedicatedScope;

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                {
                    foreach (IInjector scope in FDedicatedScope.Values)
                    {
                        scope.Dispose();
                    }

                    FDedicatedScope.Dispose();
                }

                base.Dispose(disposeManaged);
            }

            public string? Name { get; }

            public PoolServiceLifetimeManager(IScopeFactory scopeFactory, string? name)
            {
                FDedicatedScope = new ThreadLocal<IInjector>(() => scopeFactory.CreateScope(this), trackAllValues: true);
                Name = name;
            }

            public object Create()
            {
                //
                // Ez itt trukkos mert:
                //   - Minden egyes legyartott elemnek sajat scope kell (az egyes elemek kulon szalakban vannak hasznalva)
                //   - Mivel az osszes pool elemnek sajat scope-ja van ezert h az esetleges korkoros referenciat
                //     detektalni tudjuk, rekurzio eseten a mar korabban letrehozott scope-ot kell hasznaljuk.
                //

                IInjector dedicatedScope = FDedicatedScope.Value;

                dedicatedScope.Meta(PooledLifetime.POOL_SCOPE, true); // TODO: Remove

                return dedicatedScope.Get<TInterface>(Name);
            }

            public void Dispose(object item) {} // scope fogja felszabaditani

            public void CheckOut(object item) {}

            public void CheckIn(object item)
            {
                if (item is IResettable resettable && resettable.Dirty)
                    resettable.Reset();
            }

            public void RecursionDetected() {}
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
        public PoolService(IScopeFactory scopeFactory, int capacity, string? name) : base(capacity, new PoolServiceLifetimeManager(scopeFactory, name)) 
        {
            //                                            !!HACK!!
            //
            // Az egyes pool elemekhez tartozo dedikalt scope-ok felszabaditasa a pool szerviz felszabaditasakor tortenik.
            // Igy viszont ha a pool elemnek megosztott (Singleton) fuggosege van (ami meg korabban meg nem vt igenyelve)
            // az elobb kerulne feszabaditasra mint a pool elem maga.
            // Ezert meg a pool szerviz peldanyositasakor elkerunk egy pool elemet (amit egybol vissza is rakunk) hogy az
            // esetleges megosztott fuggosegek mar peldanyositasra keruljenek.
            //

            Return(Get());
        }

        protected override void Dispose(bool disposeManaged)
        {
            base.Dispose(disposeManaged); // eloszor hivjuk

            if (disposeManaged)
                ((IDisposable) LifetimeManager).Dispose();
        }

        public object Get() => Get(CheckoutPolicy.Block)!;
    }
}
