/********************************************************************************
* PoolService.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Primitives.Threading;

    internal sealed class PoolService<TInterface>: Disposable, IPool<TInterface> where TInterface: class
    {
        private sealed class PoolScopeLifetimeManager : ILifetimeManager<PoolScope<TInterface>>
        {
            public IScopeFactory ScopeFactory { get; }

            public string? Name { get; }

            public PoolScopeLifetimeManager(IScopeFactory scopeFactory, string? name)
            {
                ScopeFactory = scopeFactory;
                Name = name;
            }

            public void CheckIn(PoolScope<TInterface> item)
                => (item.ServiceInstance as IResettable)?.Reset();

            public void CheckOut(PoolScope<TInterface> item) { }

            public PoolScope<TInterface> Create()
            {
                IInjector scope = ScopeFactory.CreateScope(tag: PooledLifetime.POOL_SCOPE);
                return new PoolScope<TInterface>(scope, scope.Get<TInterface>(Name));
            }

            public void Dispose(PoolScope<TInterface> item)
                => item.Scope.Dispose();
        }

        public ObjectPool<PoolScope<TInterface>> ScopePool { get; }

        public PoolService(IScopeFactory scopeFactory, DI.PoolConfig config, string? name) 
        {
            ScopePool = new ObjectPool<PoolScope<TInterface>>
            (
                new PoolScopeLifetimeManager(scopeFactory, name),
                PoolConfig.Default with 
                {
                    Capacity = config.Capacity,
                    CheckoutPolicy = config.Blocking ? CheckoutPolicy.Block : CheckoutPolicy.Throw
                }
            );

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

        protected override void Dispose(bool disposeManaged) => ScopePool.Dispose();

        protected override ValueTask AsyncDispose() => ScopePool.DisposeAsync();

        public IPoolItem<PoolScope<TInterface>> Get() => ScopePool.Get()!;
    }
}
