/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    public partial class InjectorTests
    {
        [Test]
        public void Lifetime_TransientService_ShouldBeInstantiatedOnEveryRequest()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.IsNotNull(injector.Get<IInterface_1>());
                Assert.AreNotSame(injector.Get<IInterface_1>(), injector.Get<IInterface_1>());
            }
        }

        [Test]
        public void Lifetime_ScopedService_ShouldBeInstantiatedOnlyOncePerScope()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Scoped));

            using (IInjector injector1 = Root.CreateScope())
            {
                Assert.NotNull(injector1.Get<IInterface_1>());
                Assert.AreSame(injector1.Get<IInterface_1>(), injector1.Get<IInterface_1>());

                using (IInjector injector2 = Root.CreateScope())
                {
                    Assert.NotNull(injector2.Get<IInterface_1>());
                    Assert.AreSame(injector2.Get<IInterface_1>(), injector2.Get<IInterface_1>());
                    Assert.AreNotSame(injector1.Get<IInterface_1>(), injector2.Get<IInterface_1>());
                }
            }
        }

        [Test]
        public async Task Lifetime_ScopedService_ShouldBeInstantiatedOnlyOncePerScope_ParallelTest()
        {
            //
            // Ne mindig csak a ket Get() hivas eredmenyet hasonlitsuk ossze
            //

            var store = new ConcurrentDictionary<IInterface_1, bool>();

            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Scoped));

            await Task.WhenAll
            (
                Enumerable.Repeat(0, 100).Select(_ => Task.Factory.StartNew(() =>
                {
                    using (IInjector injector = Root.CreateScope())
                    {
                        Assert.That(store.TryAdd(injector.Get<IInterface_1>(), true));
                        Assert.False(store.TryAdd(injector.Get<IInterface_1>(), true));
                    }
                }))
            );
        }

        [Test]
        public void Lifetime_SingletonService_ShouldBeInstantiatedOnlyOnce()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Singleton));

            using (IInjector injector1 = Root.CreateScope())
            {
                using (IInjector injector2 = Root.CreateScope())
                {
                    Assert.IsNotNull(injector1.Get<IInterface_1>());
                    Assert.IsNotNull(injector2.Get<IInterface_1>());

                    Assert.AreSame(injector1.Get<IInterface_1>(), injector1.Get<IInterface_1>());
                    Assert.AreSame(injector2.Get<IInterface_1>(), injector2.Get<IInterface_1>());
                    Assert.AreSame(injector2.Get<IInterface_1>(), injector2.Get<IInterface_1>());
                }
            }
        }

        [Test]
        public async Task Lifetime_SingletonService_ShouldBeInstantiatedOnlyOnceInTheRootScope_ParallelTest()
        {
            //
            // Ne mindig csak a ket Get() hivas eredmenyet hasonlitsuk ossze
            //

            var store = new ConcurrentDictionary<IInterface_1, bool>();

            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Singleton));

            await Task.WhenAll
            (
                Enumerable.Repeat(0, 100).Select(_ => Task.Factory.StartNew(() =>
                {
                    using (IInjector injector = Root.CreateScope())
                    {
                        store.TryAdd(injector.Get<IInterface_1>(), true);
                    }
                }))
            );

            Assert.That(store.Count, Is.EqualTo(1));
        }

        [Test]
        public void Lifetime_SingletonService_MayHaveScopedDependency()
        {
            Disposable instance;

            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs
                .Service<IDisposable, MyDisposable>(Lifetime.Scoped)
                .Service<IInterface_7<IDisposable>, Implementation_7_TInterface_Dependant<IDisposable>>(Lifetime.Singleton)))
            {
                using (IInjector injector = root.CreateScope())
                {
                    injector.Get<IInterface_7<IDisposable>>();
                }

                using (IInjector injector = root.CreateScope())
                {
                    instance = (Disposable) injector.Get<IInterface_7<IDisposable>>().Interface;
                    Assert.That(instance.Disposed, Is.False);
                }
            }

            Assert.That(instance.Disposed);
        }

        [Test]
        public void Lifetime_SingletonService_ShouldBeResolvedFromTheRootScope() 
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInjector>, Implementation_7_TInterface_Dependant<IInjector>>(Lifetime.Singleton));

            using (IInjector injector = Root.CreateScope()) 
            {
                IInjector dedicatedInjector = injector.Get<IInterface_7<IInjector>>().Interface;

                Assert.That(dedicatedInjector, Is.SameAs(Root));
            }
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public void Lifetime_NonSingletonService_ShouldBeInstantiatedInTheCurrentScope(Lifetime lifetime) 
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInjector>, Implementation_7<IInjector>>(lifetime));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.Get<IInterface_7<IInjector>>().Interface, Is.SameAs(injector));
            }
        }

        [Test]
        public void Lifetime_SingletonService_ShouldBeInstantiatedInTheRootScope()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInjector>, Implementation_7<IInjector>>(Lifetime.Singleton));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.Get<IInterface_7<IInjector>>().Interface, Is.SameAs(Root));
            }
        }

        [Test]
        public void Lifetime_PooledService_ShouldBeInstantiatedUpToNTimes([Values(1, 2, 3)] int times)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Pooled.Using(PoolConfig.Default with { Capacity = times })));

            ManualResetEventSlim stop = new ManualResetEventSlim();

            Task[] holders = Enumerable.Repeat(0, times).Select(_ => Task.Run(() =>
            {
                using (IInjector injector = Root.CreateScope())
                {
                    Assert.AreSame(injector.Get<IInterface_1>(), injector.Get<IInterface_1>());

                    //
                    // Nem tesszuk vissza a pool-ba
                    //

                    stop.Wait();
                }
            })).ToArray();

            Thread.Sleep(100);

            Task extra = Task.Run(() =>
            {
                using (IInjector injector = Root.CreateScope())
                {
                    injector.Get<IInterface_1>();
                }
            });

            Assert.False(extra.Wait(100));

            stop.Set();
      
            Assert.True(extra.Wait(100));

            //
            // Mielott felszabaditanank a gyoker kontenert megvarjuk amig mindenki leall.
            //

            Task.WaitAll(holders);
        }

        [Test]
        public async Task Lifetime_PooledService_ShouldHaveItsOwnScope()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInjector>, Implementation_7_TInterface_Dependant<IInjector>>(Lifetime.Pooled.Using(PoolConfig.Default with { Capacity = 2 })));

            using (IInjector injector1 = Root.CreateScope())
            {
                IInterface_7<IInjector> svc1 = injector1.Get<IInterface_7<IInjector>>();

                Assert.That(svc1.Interface, Is.Not.SameAs(injector1));

                await Task.Run(() => // kulon szal kell hogy ne ugyanazt a szerviz peldanyt kapjuk vissza
                {
                    using (IInjector injector2 = Root.CreateScope())
                    {
                        IInterface_7<IInjector> svc2 = injector2.Get<IInterface_7<IInjector>>();

                        Assert.That(svc2.Interface, Is.Not.SameAs(injector2));
                        Assert.AreNotSame(svc1.Interface, svc2.Interface);
                    }
                });
            }
        }

        private interface IDisposableService : IDisposableEx { }

        private class DisposableService : Disposable, IDisposableService, IInterface_1 { }

        [Test]
        public void Lifetime_PooledService_ShouldBeDisposedOnRootDisposal()
        {
            IDisposableEx disposable;

            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs.Service<IDisposableService, DisposableService>(Lifetime.Pooled)))
            {
                using (IInjector injector = root.CreateScope())
                {
                    disposable = injector.Get<IDisposableService>();
                }

                Assert.False(disposable.Disposed);
            }

            Assert.That(disposable.Disposed);
        }

        [Test]
        public void Lifetime_PooledService_MayHavePooledDependency() // igazabol ezt a StrictDI tiltja
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Pooled)
                .Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(Lifetime.Pooled));

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_7<IInterface_1> svc = injector.Get<IInterface_7<IInterface_1>>();

                Assert.That(svc.Interface, Is.Not.Null);
            }
        }

        [Test]
        public void Lifetime_PooledService_MayHaveRegularDependency()
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(Lifetime.Pooled));

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_7<IInterface_1> svc = injector.Get<IInterface_7<IInterface_1>>();

                Assert.That(svc.Interface, Is.Not.Null);
            }
        }

        private class DisposableServiceHavingDisposableDependency : Disposable, IDisposableService 
        {
            public IDisposableEx Dependency { get; }

            public bool DependencyWasDisposed { get; private set; }

            public DisposableServiceHavingDisposableDependency(IDisposableEx disposableDep) => Dependency = disposableDep;

            protected override void Dispose(bool disposeManaged) =>
                DependencyWasDisposed = Dependency.Disposed;
        }

        [Test]
        public void Lifetime_PooledService_CanAccessItsDependencyOnDispose([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            DisposableServiceHavingDisposableDependency svc;

            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs
                .Service<IDisposableEx, MyDisposable>(lifetime)
                .Service<IDisposableService, DisposableServiceHavingDisposableDependency>(Lifetime.Pooled)))
            {
                using (IInjector injector = root.CreateScope())
                {
                    svc = (DisposableServiceHavingDisposableDependency) injector.Get<IDisposableService>();
                }

                Assert.That(svc.Disposed, Is.False);
                Assert.That(svc.Dependency.Disposed, Is.False);
            }

            Assert.That(svc.Disposed);
            Assert.That(svc.Dependency.Disposed);
            Assert.That(svc.DependencyWasDisposed, Is.False);
        }

        [Test]
        public void Lifetime_PooledService_PooledDependencyShouldBeAccessibleOnDispose([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            DisposableServiceHavingDisposableDependency svc;

            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs
                .Service<IDisposableEx, MyDisposable>(Lifetime.Pooled)
                .Service<IDisposableService, DisposableServiceHavingDisposableDependency>(lifetime)))
            {
                using (IInjector injector = root.CreateScope())
                {
                    svc = (DisposableServiceHavingDisposableDependency) injector.Get<IDisposableService>();
                }

                Assert.That(svc.Dependency.Disposed, Is.False);
            }

            Assert.That(svc.Disposed);
            Assert.That(svc.Dependency.Disposed);
            Assert.That(svc.DependencyWasDisposed, Is.False);
        }

        [Test]
        public async Task Lifetime_PooledService_MayBeGeneric([Values(null, "cica")] string name, [ValueSource(nameof(Lifetimes))] Lifetime depLifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(depLifetime)
                .Service(typeof(IInterface_3<>), name, typeof(Implementation_3_IInterface_1_Dependant<>), Lifetime.Pooled.Using(PoolConfig.Default with { Capacity = 2 })));

            using (IInjector injector1 = Root.CreateScope())
            {
                Assert.AreSame(injector1.Get<IInterface_3<int>>(name), injector1.Get<IInterface_3<int>>(name));
                Assert.AreSame(injector1.Get<IInterface_3<string>>(name), injector1.Get<IInterface_3<string>>(name));

                await Task.Run(() =>
                {
                    using (IInjector injector2 = Root.CreateScope())
                    {
                        Assert.AreSame(injector2.Get<IInterface_3<int>>(name), injector2.Get<IInterface_3<int>>(name));
                        Assert.AreSame(injector2.Get<IInterface_3<string>>(name), injector2.Get<IInterface_3<string>>(name));

                        Assert.AreNotSame(injector1.Get<IInterface_3<int>>(name), injector2.Get<IInterface_3<int>>(name));
                        Assert.AreNotSame(injector1.Get<IInterface_3<string>>(name), injector2.Get<IInterface_3<string>>(name));
                    }
                });
            }
        }

        [Test]
        public void Lifetime_PermissiveDI_LegalCases(
            [ValueSource(nameof(Lifetimes))] Lifetime dependant,
            [ValueSource(nameof(Lifetimes))] Lifetime dependency)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(dependency)
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(dependant));

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = Root.CreateScope())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_PermissiveDI_LegalCases([ValueSource(nameof(Lifetimes))] Lifetime dependant)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Instance<IInterface_1>(new Implementation_1_No_Dep())
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(dependant));

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = Root.CreateScope())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_LegalCases1(
            [ValueSource(nameof(ScopeControlledLifetimes))] Lifetime dependant,
            [ValueSource(nameof(Lifetimes))] Lifetime dependency) 
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(dependency)
                    .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(dependant),
                new ScopeOptions { StrictDI = true }
            );

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = Root.CreateScope())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_LegalCases2([ValueSource(nameof(ScopeControlledLifetimes))] Lifetime dependant)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Instance<IInterface_1>(new Implementation_1_No_Dep())
                    .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(dependant),
                new ScopeOptions { StrictDI = true }
            );

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = Root.CreateScope())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_LegalCases3()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Singleton)
                    .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Singleton),
                new ScopeOptions { StrictDI = true }
            );

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = Root.CreateScope())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_IllegalCases1_JIT(
            [ValueSource(nameof(ScopeControlledLifetimes))] Lifetime dependency,
            [ValueSource(nameof(RootControlledLifetimes))] Lifetime requestor) 
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(dependency)
                    .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(requestor),
                new ScopeOptions { StrictDI = true, ServiceResolutionMode = ServiceResolutionMode.JIT }
            );

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = Root.CreateScope())
                {
                    Assert.Throws<RequestNotAllowedException>(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_IllegalCases1_AOT(
            [ValueSource(nameof(ScopeControlledLifetimes))] Lifetime dependency,
            [ValueSource(nameof(RootControlledLifetimes))] Lifetime requestor) => Assert.Throws<RequestNotAllowedException>
        (
            () => ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(dependency)
                    .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(requestor),
                new ScopeOptions { StrictDI = true, ServiceResolutionMode = ServiceResolutionMode.AOT }
            )
        );

        [Test]
        public void Lifetime_StrictDI_IllegalCases2_JIT()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Pooled)
                    .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Pooled),
                new ScopeOptions { StrictDI = true, ServiceResolutionMode = ServiceResolutionMode.JIT }
            );

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = Root.CreateScope())
                {
                    Assert.Throws<RequestNotAllowedException>(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_IllegalCases2_AOT() => Assert.Throws<RequestNotAllowedException>
        (
            () => ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Pooled)
                    .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Pooled),
                new ScopeOptions { StrictDI = true, ServiceResolutionMode = ServiceResolutionMode.AOT }
            )
        );

        [Test]
        public void Lifetime_StrictDI_IllegalCases3_JIT([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs.Service<IInterface_7<IInjector>, Implementation_7<IInjector>>(lifetime),
                new ScopeOptions { StrictDI = true, ServiceResolutionMode = ServiceResolutionMode.JIT }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<RequestNotAllowedException>(() => injector.Get<IInterface_7<IInjector>>());
            }
        }

        [Test]
        public void Lifetime_StrictDI_IllegalCases3_AOT([ValueSource(nameof(Lifetimes))] Lifetime lifetime) => Assert.Throws<RequestNotAllowedException>
        (
            () => ScopeFactory.Create
            (
                svcs => svcs.Service<IInterface_7<IInjector>, Implementation_7<IInjector>>(lifetime),
                new ScopeOptions { StrictDI = true, ServiceResolutionMode = ServiceResolutionMode.AOT }
            )
        );

        [Test]
        public void Lifetime_StrictDI_IllegalCases4_JIT([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs.Service<IInterface_7<IServiceProvider>, Implementation_7<IServiceProvider>>(lifetime),
                new ScopeOptions { StrictDI = true, ServiceResolutionMode = ServiceResolutionMode.JIT, SupportsServiceProvider = true }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<RequestNotAllowedException>(() => injector.Get<IInterface_7<IServiceProvider>>());
            }
        }

        [Test]
        public void Lifetime_StrictDI_IllegalCases4_AOT([ValueSource(nameof(Lifetimes))] Lifetime lifetime) => Assert.Throws<RequestNotAllowedException>
        (
            () => ScopeFactory.Create
            (
                svcs => svcs.Service<IInterface_7<IServiceProvider>, Implementation_7<IServiceProvider>>(lifetime),
                new ScopeOptions { StrictDI = true, ServiceResolutionMode = ServiceResolutionMode.AOT, SupportsServiceProvider = true }
            )
        );

        [Test]
        public void Lifetime_ShouldBeNullChecked([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(null, null, typeof(Disposable), ServiceOptions.Default).ToList());
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(typeof(IDisposable), null, (Type) null, ServiceOptions.Default).ToList());
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(typeof(IDisposable), null, typeof(Disposable), null).ToList());
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(typeof(IDisposable), null, typeof(Disposable), null, ServiceOptions.Default).ToList());
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(typeof(IDisposable), null, typeof(Disposable), new object(), null).ToList());
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(typeof(IDisposable), null, null, new object(), ServiceOptions.Default).ToList());
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(null, null, typeof(Disposable), new object(), ServiceOptions.Default).ToList());
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(typeof(IDisposable), null, (Expression<FactoryDelegate>) null, ServiceOptions.Default).ToList());
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(null, null, (_, __) => new object(), ServiceOptions.Default).ToList());
            Assert.Throws<ArgumentNullException>(() => lifetime.CreateFrom(typeof(IDisposable), null, (_, __) => new object(), null).ToList());
        }

        [Test]
        public void Lifetime_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => new InstanceLifetime().CreateFrom(typeof(IDisposable), null, (Disposable) null, ServiceOptions.Default).ToList());
            Assert.Throws<ArgumentNullException>(() => new InstanceLifetime().CreateFrom(null, null, new Disposable(), ServiceOptions.Default).ToList());
            Assert.Throws<ArgumentNullException>(() => new InstanceLifetime().CreateFrom(typeof(IDisposable), null, new Disposable(), null).ToList());
        }

        [Test]
        public void LifetimeBase_ShouldThrowNotSupportCreate()
        {
            LifetimeBase lifetime = new Mock<LifetimeBase>() { CallBase = true }.Object;
            Assert.Throws<NotSupportedException>(() => lifetime.CreateFrom(typeof(IDisposable), null, new Disposable(), ServiceOptions.Default));
            Assert.Throws<NotSupportedException>(() => lifetime.CreateFrom(typeof(IDisposable), null, typeof(Disposable), ServiceOptions.Default));
            Assert.Throws<NotSupportedException>(() => lifetime.CreateFrom(typeof(IDisposable), null, typeof(Disposable), new object(), ServiceOptions.Default));
            Assert.Throws<NotSupportedException>(() => lifetime.CreateFrom(typeof(IDisposable), null, (_, __) => new Disposable(false), ServiceOptions.Default));
            Assert.Throws<NotSupportedException>(() => lifetime.Using(new object()));
        }
    }
}
