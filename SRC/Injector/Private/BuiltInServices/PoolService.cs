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

    internal sealed class PoolService<TInterface> : ObjectPool<object>, IPool<TInterface> where TInterface: class
    {
        private sealed class ServiceReferenceLifetimeManager: Disposable, ILifetimeManager<object>
        {
            //
            // Mivel az osszes pool elemnek sajat scope-ja van ezert h az esetleges korkoros referenciat
            // detektalni tudjuk, rekurzio eseten a mar korabban letrehozott scope-ot kell hasznaljuk.
            //
            // Ne [ThreadStatic]-t hasznaljunk, hogy minden interface-nev paroshoz kulon peldany tartozzon.
            //

            private readonly ThreadLocal<IInjector?> FCurrentScope = new(trackAllValues: false);

            public IScopeFactory ScopeFactory { get; }

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FCurrentScope.Dispose();

                base.Dispose(disposeManaged);
            }

            public string? Name { get; }

            public ServiceReferenceLifetimeManager(IScopeFactory scopeFactory, string? name)
            {
                ScopeFactory = scopeFactory;
                Name = name;
            }

            public object Create()
            {
                FCurrentScope.Value ??= ScopeFactory.CreateSystemScope();
                try
                {
                    //
                    // Ez itt trukkos mert:
                    //   1) "injector" by design nem szalbiztos viszont ez a metodus lehet hivva paralell
                    //   2) Minden egyes legyartott elemnek sajat scope kell (az egyes elemek kulon szalakban lehetnek hasznalva)
                    //

                    FCurrentScope.Value.Meta(PooledLifetime.POOL_SCOPE, true);

                    return FCurrentScope.Value.Get(typeof(TInterface), Name);
                }
                finally
                {
                    FCurrentScope.Value = null;
                }
            }

            public void Dispose(object item) {} //scope fogja felszabaditani

            public void CheckOut(object item) {}

            public void CheckIn(object item)
            {
                if (item is IResettable resettable && resettable.Dirty)
                    resettable.Reset();
            }

            public void RecursionDetected() {}
        }

        private sealed class Wrapped : Disposable, IWrapped<object>
        {
            private readonly object FValue;

            public Wrapped(ObjectPool<object> owner)
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

            public ObjectPool<object> Owner { get; }

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
        public PoolService(IScopeFactory scopeFactory, int capacity, string? name) : base(capacity, new ServiceReferenceLifetimeManager(scopeFactory, name)) {}

        protected override void Dispose(bool disposeManaged)
        {
            base.Dispose(disposeManaged); // eloszor hivjuk

            if (disposeManaged)
                ((IDisposable) LifetimeManager).Dispose();
        }

        public IWrapped<object> Get() => new Wrapped(this);
    }
}
