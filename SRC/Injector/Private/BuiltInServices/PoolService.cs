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

    //                                        !!!ATTENTION!!!
    //
    // This class is a critical component therefore every modification should be done carefully, with
    // performance in mind.
    // - NO System.Linq
    // - NO System.Reflection
    // - After ANY modifications, run the unit & performance tests to verify there is no regression
    //

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

            //
            // The pool service is instantiated in the root scope -obviously- before any of the pool items.
            // Therefore all the non-instantiated singleton dependencies would be released before the pool
            // item, which is not acceptable. To avoid this situation force a pool item to be created ahead
            // of time.
            //
            // This workaround won't imply any performance impact as the pool item would be created anyway
            // at first checkout.
            //

            Get().Dispose();
        }

        protected override void Dispose(bool disposeManaged) => ScopePool.Dispose();

        protected override ValueTask AsyncDispose() => ScopePool.DisposeAsync();

        public IPoolItem<PoolScope<TInterface>> Get() => ScopePool.Get()!;
    }
}
