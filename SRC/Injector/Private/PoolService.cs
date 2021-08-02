/********************************************************************************
* PoolService.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Primitives.Threading;

    internal sealed class PoolService<TInterface> : ObjectPool<IServiceReference>, IPool<TInterface> where TInterface: class
    {
        private sealed class ServiceReferenceLifetimeManager : ILifetimeManager<IServiceReference>
        {
            //
            // Mivel az osszes pool elemnek sajat scope-ja van ezert h az esetleges korkoros referenciat
            // detektalni tudjuk, rekurzio eseten a mar korabban letrehozott scope-ot kell hasznaljuk.
            //

            [ThreadStatic]
            private static IInjector? FCurrentScope;

            public IScopeFactory ScopeFactory { get; }

            public string? Name { get; }

            public ServiceReferenceLifetimeManager(IScopeFactory scopeFactory, string? name)
            {
                ScopeFactory = scopeFactory;
                Name = name;
            }

            public IServiceReference Create()
            {
                FCurrentScope ??= ScopeFactory.CreateScope();
                try
                {
                    //
                    // Ez itt trukkos mert:
                    //   1) "injector" by design nem szalbiztos viszont ez a metodus lehet hivva paralell
                    //   2) Minden egyes legyartott elemnek sajat scope kell (az egyes elemek kulon szalakban lehetnek hasznalva)
                    //   3) Letrehozaskor a mar meglevo grafot boviteni kell 
                    //

                    FCurrentScope.Meta(PooledLifetime.POOL_SCOPE, true);

                    return FCurrentScope.GetReference(typeof(TInterface), Name);
                }
                finally
                {
                    FCurrentScope = null;
                }
            }

            public void Dispose(IServiceReference item)
            {
            }

            public void CheckOut(IServiceReference item)
            {
                item.AddRef();
            }

            public void CheckIn(IServiceReference item)
            {
                if (item.Value is IResettable resettable && resettable.Dirty)
                    resettable.Reset();

                item.Release();
            }

            public void RecursionDetected()
            {
            }
        }

        public PoolService(IScopeFactory scopeFactory, int capacity, string? name) : base(capacity, new ServiceReferenceLifetimeManager(scopeFactory, name)) {}

        public PoolItem<IServiceReference> Get() => this.GetItem(CheckoutPolicy.Block)!;
    }
}
