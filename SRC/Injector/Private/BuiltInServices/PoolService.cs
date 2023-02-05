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
        private sealed class PoolItem : Disposable, IPoolItem<TInterface>
        {
            public PoolItem(PoolService<TInterface> owner)
            {
                Scope = owner.FScopePool.Get(owner.FCheckoutPolicy)!;
                Value = Scope.Get<TInterface>(owner.FName);
                Owner = owner;
            }

            protected override void Dispose(bool disposeManaged)
            {
                if (Value is IResettable resettable && resettable.Dirty)
                    resettable.Reset();

                Owner.FScopePool.Return(Scope);
            }

            public PoolService<TInterface> Owner { get; }

            public IInjector Scope { get; }

            public TInterface Value { get; }
        }

        private readonly CheckoutPolicy FCheckoutPolicy;

        private readonly string? FName;

        private readonly ObjectPool<IInjector> FScopePool;

        public PoolService(IScopeFactory scopeFactory, PoolConfig config, string? name) 
        {
            FScopePool = new ObjectPool<IInjector>(() =>scopeFactory.CreateScope(tag: PooledServiceEntry.POOL_SCOPE), config.Capacity);
            FCheckoutPolicy = config.Blocking ? CheckoutPolicy.Block : CheckoutPolicy.Throw;
            FName = name;

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

        protected override void Dispose(bool disposeManaged) => FScopePool.Dispose();

        protected override ValueTask AsyncDispose() => FScopePool.DisposeAsync();

        public IPoolItem<TInterface> Get() => new PoolItem(this);
    }
}
