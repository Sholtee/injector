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

    internal sealed class PoolService<TInterface> : ObjectPool<IServiceReference>, IPool<TInterface> where TInterface: class
    {
        private sealed class ServiceReferenceLifetimeManager: Disposable, ILifetimeManager<IServiceReference>
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

            public IServiceReference Create()
            {
                FCurrentScope.Value ??= ScopeFactory.CreateScope();
                try
                {
                    //
                    // Ez itt trukkos mert:
                    //   1) "injector" by design nem szalbiztos viszont ez a metodus lehet hivva paralell
                    //   2) Minden egyes legyartott elemnek sajat scope kell (az egyes elemek kulon szalakban lehetnek hasznalva)
                    //   3) Letrehozaskor a mar meglevo grafot boviteni kell 
                    //

                    FCurrentScope.Value.Meta(PooledLifetime.POOL_SCOPE, true);

                    return FCurrentScope.Value.GetReference(typeof(TInterface), Name);
                }
                finally
                {
                    FCurrentScope.Value = null;
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

        private sealed class Wrapped : Disposable, IWrapped<object>
        {
            public Wrapped(ObjectPool<IServiceReference> owner)
            {
                Owner = owner;
                Reference = owner.Get(CheckoutPolicy.Block)!;
            }

            protected override void Dispose(bool disposeManaged)
            {
                CheckNotDisposed();

                if (disposeManaged)
                    Owner.Return(Reference);

                base.Dispose(disposeManaged);
            }

            public ObjectPool<IServiceReference> Owner { get; }

            public IServiceReference Reference { get; }

            public object Value
            {
                get
                {
                    CheckNotDisposed();
                    return Reference.Value!;
                }
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "LifetimeManager is released in the Dispose(bool) method")]
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
