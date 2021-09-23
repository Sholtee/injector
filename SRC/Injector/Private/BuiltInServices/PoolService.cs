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

    internal sealed class PoolService<TInterface> : ObjectPool<TInterface>, IPool<TInterface> where TInterface: class
    {
        private sealed class PoolServiceLifetimeManager: Disposable, ILifetimeManager<TInterface>
        {
            //
            // Ne [ThreadStatic]-t hasznaljunk, hogy minden interface-nev paroshoz kulon peldany tartozzon.
            //

            private readonly ThreadLocal<IInjector?> FDedicatedScope = new(trackAllValues: true);

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                {
                    foreach (IInjector? scope in FDedicatedScope.Values)
                    {
                        scope?.Dispose();
                    }

                    FDedicatedScope.Dispose();
                }

                base.Dispose(disposeManaged);
            }

            public IScopeFactory ScopeFactory { get; }

            public string? Name { get; }

            public PoolServiceLifetimeManager(IScopeFactory scopeFactory, string? name)
            {
                ScopeFactory = scopeFactory;
                Name = name;
            }

            public TInterface Create()
            {
                //
                // Ez itt trukkos mert:
                //   - Minden egyes legyartott elemnek sajat scope kell (az egyes elemek kulon szalakban vannak hasznalva)
                //   - Mivel az osszes pool elemnek sajat scope-ja van ezert h az esetleges korkoros referenciat
                //     detektalni tudjuk, rekurzio eseten a mar korabban letrehozott scope-ot kell hasznaljuk.
                //

                IInjector dedicatedScope = FDedicatedScope.Value ??= ScopeFactory.CreateScope();

                dedicatedScope.Meta(PooledLifetime.POOL_SCOPE, true);

                return dedicatedScope.Get<TInterface>(Name);
            }

            public void Dispose(TInterface item) {} //scope fogja felszabaditani

            public void CheckOut(TInterface item) {}

            public void CheckIn(TInterface item)
            {
                if (item is IResettable resettable && resettable.Dirty)
                    resettable.Reset();
            }

            public void RecursionDetected() {}
        }

        private sealed class Wrapped : Disposable, IWrapped<object> // NE IWrapped<TInterface> legyen mert azt a rendszer nem ismeri
        {
            private readonly TInterface FValue;

            public Wrapped(ObjectPool<TInterface> owner)
            {
                Owner = owner;
                FValue = owner.Get(CheckoutPolicy.Block)!;
            }

            protected override void Dispose(bool disposeManaged)
            {
                CheckNotDisposed();

                if (disposeManaged)
                    Owner.Return(FValue);

                base.Dispose(disposeManaged);
            }

            public ObjectPool<TInterface> Owner { get; }

            public object Value
            {
                get 
                {
                    CheckNotDisposed();
                    return FValue;
                }
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
        public PoolService(IScopeFactory scopeFactory, int capacity, string? name) : base(capacity, new PoolServiceLifetimeManager(scopeFactory, name)) 
        {
            //
            //                                            !!HACK!!
            //
            // Az egyes pool elemekhez tartozo dedikalt scope-ok felszabaditasa a pool szerviz felszabaditasakor tortenik.
            // Igy viszont ha a pool elemnek megosztott (Singleton) fuggosege van (ami meg korabban meg nem vt igenyelve)
            // az elobb kerulne feszabaditasra mint a pool elem maga.
            // Ezert meg a pool szerviz peldanyositasakor elkerunk egy pool elemet (amit egybol vissza is rakunk) hogy az
            // esetleges megosztott fuggosegek mar peldanyositasra keruljenek.
            //

            Get().Dispose();
        }

        protected override void Dispose(bool disposeManaged)
        {
            base.Dispose(disposeManaged); // eloszor hivjuk

            if (disposeManaged)
                ((IDisposable) LifetimeManager).Dispose();
        }

        public IWrapped<object> Get() => new Wrapped(this);
    }
}
