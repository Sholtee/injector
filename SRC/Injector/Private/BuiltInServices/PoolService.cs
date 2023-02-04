/********************************************************************************
* PoolService.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Primitives.Threading;

    internal sealed class PoolService<TInterface> : ObjectPool<object>, IPool<TInterface> where TInterface: class
    {
        private sealed class PoolServiceLifetimeManager: Disposable, ILifetimeManager<object>
        {
            //
            // Don't use [ThreadStatic] here as every interface-name pair requires its own instance.
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
                FDedicatedScope = new ThreadLocal<IInjector>
                (
                    () => scopeFactory.CreateScope(tag: PooledServiceEntry.POOL_SCOPE), 
                    trackAllValues: true
                );
                Name = name;
            }

            public object Create()
            {
                //
                // It's tricky since:
                //   - Every produced item requires a separate scope (as they will be used in separate threads)
                //   - To let the system detect circular references -in case of recursion- we must not return a new instance.
                //

                IInjector dedicatedScope = FDedicatedScope.Value;

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

        private readonly CheckoutPolicy FCheckoutPolicy;

        public PoolService(IScopeFactory scopeFactory, PoolConfig config, string? name) : base(new PoolServiceLifetimeManager(scopeFactory, name), config.Capacity) 
        {
            FCheckoutPolicy = config.Blocking ? CheckoutPolicy.Block : CheckoutPolicy.Throw;

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

        public object Get() => Get(FCheckoutPolicy)!;
    }
}
