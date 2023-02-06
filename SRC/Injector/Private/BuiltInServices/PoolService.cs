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
                Scope = owner.ScopePool.Get(owner.CheckoutPolicy)!;
                Value = Scope.Get<TInterface>(owner.Name);
                Owner = owner;
            }

            protected override void Dispose(bool disposeManaged)
            {
                if (Value is IResettable resettable && resettable.Dirty)
                    resettable.Reset();

                Owner.ScopePool.Return(Scope);
            }

            public PoolService<TInterface> Owner { get; }

            public IInjector Scope { get; }

            public TInterface Value { get; }
        }

        public CheckoutPolicy CheckoutPolicy { get; }

        public string? Name { get; }

        public ObjectPool<IInjector> ScopePool { get; }

        public PoolService(IScopeFactory scopeFactory, PoolConfig config, string? name) 
        {
            ScopePool = new ObjectPool<IInjector>(() => scopeFactory.CreateScope(tag: PooledLifetime.POOL_SCOPE), config.Capacity);
            CheckoutPolicy = config.Blocking ? CheckoutPolicy.Block : CheckoutPolicy.Throw;
            Name = name;

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

        public IPoolItem<TInterface> Get() => new PoolItem(this);
    }
}
