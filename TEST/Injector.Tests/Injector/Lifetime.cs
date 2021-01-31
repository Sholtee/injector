/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;
    using Properties;

    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Lifetime_TransientService_ShouldBeInstantiatedOnEveryRequest()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.AreNotSame(injector.Get<IInterface_1>(), injector.Get<IInterface_1>());
            }
        }

        [Test]
        public void Lifetime_TransientService_ShouldNotBeInstantiatedIfTheInjectorWasRecycled() 
        {
            Config.Value.Injector.MaxSpawnedTransientServices = 1;

            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            using (IInjector injector1 = Container.CreateInjector())
            {
                Assert.DoesNotThrow(() => injector1.Get<IInterface_1>());
                Assert.Throws<Exception>(() => injector1.Get<IInterface_1>(), Resources.INJECTOR_SHOULD_BE_RELEASED);

                //
                // Ettol meg masik injector tud peldanyositani.
                //

                using (IInjector injector2 = Container.CreateInjector())
                {
                    Assert.DoesNotThrow(() => injector2.Get<IInterface_1>());
                }
            }
        }

        [Test]
        public void Lifetime_InheritedTransientService_ShouldBeInstantiatedOnEveryRequest()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            using (IInjector injector = Container.CreateChild().CreateInjector())
            {
                Assert.AreNotSame(injector.Get<IInterface_1>(), injector.Get<IInterface_1>());
            }
        }

        [Test]
        public void Lifetime_ScopedService_ShouldBeInstantiatedOnlyOncePerInjector()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Scoped);

            using (IInjector injector1 = Container.CreateInjector())
            {
                Assert.AreSame(injector1.Get<IInterface_1>(), injector1.Get<IInterface_1>());

                using (IInjector injector2 = Container.CreateInjector())
                {
                    Assert.AreSame(injector2.Get<IInterface_1>(), injector2.Get<IInterface_1>());
                    Assert.AreNotSame(injector1.Get<IInterface_1>(), injector2.Get<IInterface_1>());
                }
            }
        }

        [Test]
        public async Task Lifetime_ScopedService_ShouldBeInstantiatedOnlyOncePerInjector_ParallelTest()
        {
            //
            // Ne mindig csak a ket Get() hivas eredmenyet hasonlitsuk ossze
            //

            var store = new ConcurrentDictionary<IInterface_1, bool>();

            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Scoped);

            await Task.WhenAll
            (
                Enumerable.Repeat(0, 100).Select(_ => Task.Factory.StartNew(() =>
                {
                    using (IInjector injector = Container.CreateInjector())
                    {
                        Assert.That(store.TryAdd(injector.Get<IInterface_1>(), true));
                        Assert.False(store.TryAdd(injector.Get<IInterface_1>(), true));
                    }
                }))
            );
        }

        [Test]
        public void Lifetime_InheritedScopedService_ShouldBeInstantiatedOnlyOncePerInjector()
        {
            IServiceContainer childContainer = Container // nem muszaj dispose-olni Container felszabaditasakor ugy is dispose-olva lesz
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Scoped)
                .CreateChild();

            using (IInjector injector1 = childContainer.CreateInjector())
            {
                Assert.AreSame(injector1.Get<IInterface_1>(), injector1.Get<IInterface_1>());

                using (IInjector injector2 = childContainer.CreateInjector())
                {
                    Assert.AreSame(injector2.Get<IInterface_1>(), injector2.Get<IInterface_1>());
                    Assert.AreNotSame(injector1.Get<IInterface_1>(), injector2.Get<IInterface_1>());
                }
            }
        }

        [Test]
        public void Lifetime_SingletonService_ShouldBeInstantiatedOnlyOncePerDeclaringContainer()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Singleton);

            using (IInjector injector1 = Container.CreateInjector())
            {
                using (IInjector injector2 = Container.CreateChild().CreateInjector())
                {
                    using (IInjector injector3 = Container.CreateChild().CreateChild().CreateInjector())
                    {                    
                        Assert.AreSame(injector1.Get<IInterface_1>(), injector3.Get<IInterface_1>());
                        Assert.AreSame(injector2.Get<IInterface_1>(), injector3.Get<IInterface_1>());
                        Assert.AreSame(injector3.Get<IInterface_1>(), injector3.Get<IInterface_1>());
                    }
                }
            }
        }

        [Test]
        public async Task Lifetime_SingletonService_ShouldBeInstantiatedOnlyOncePerDeclaringContainer_ParallelTest()
        {
            //
            // Ne mindig csak a ket Get() hivas eredmenyet hasonlitsuk ossze
            //

            var store = new ConcurrentDictionary<IInterface_1, bool>();

            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Singleton);

            await Task.WhenAll
            (
                Enumerable.Repeat(0, 100).Select(_ => Task.Factory.StartNew(() =>
                {
                    using (IInjector injector = Container.CreateInjector())
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
            Config.Value.Injector.StrictDI = false;

            Disposable instance;

            using (IServiceContainer child = Container.CreateChild())
            {
                child
                    .Service<IDisposable, Disposable>(Lifetime.Scoped)
                    .Service<IInterface_7<IDisposable>, Implementation_7_TInterface_Dependant<IDisposable>>(Lifetime.Singleton);

                using (IInjector injector = child.CreateInjector())
                {
                    injector.Get<IInterface_7<IDisposable>>();
                }

                using (IInjector injector = child.CreateInjector())
                {
                    instance = (Disposable) injector.Get<IInterface_7<IDisposable>>().Interface;
                    Assert.That(instance.Disposed, Is.False);
                }
            }

            Assert.That(instance.Disposed);
        }

        [Test]
        public void Lifetime_SingletonService_ShouldHaveItsOwnInjector() 
        {
            Container
                .Service<IInterface_7<IInjector>, Implementation_7_TInterface_Dependant<IInjector>>(Lifetime.Singleton)
                .Service<IInterface_7<IInjector>, Implementation_7_TInterface_Dependant<IInjector>>("named", Lifetime.Singleton);

            using (IInjector injector = Container.CreateInjector()) 
            {
                IInterface_7<IInjector> svc = injector.Get<IInterface_7<IInjector>>("named");

                Assert.That(svc.Interface, Is.Not.SameAs(injector));
                Assert.That(svc.Interface.UnderlyingContainer.Parent, Is.SameAs(Container));

                Assert.That(svc.Interface.Get<IInterface_7<IInjector>>().Interface, Is.Not.SameAs(injector));
                Assert.That(svc.Interface.Get<IInterface_7<IInjector>>().Interface.UnderlyingContainer.Parent, Is.SameAs(Container));

                Assert.That(svc.Interface.Get<IInterface_7<IInjector>>().Interface, Is.Not.SameAs(svc.Interface));
            }
        }

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Lifetime_NonSingletonService_ShouldResolveDependencyFromTheParentContainer(Lifetime lifetime) 
        {
            Container.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(lifetime);

            IServiceContainer child = Container.CreateChild();
            child.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            using (IInjector injector = child.CreateInjector())
            {
                Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
            }
        }

        [Test]
        public void Lifetime_SingletonService_ShouldResolveDependencyFromTheDeclaringContainer_DeclarationTest()
        {
            Container.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Singleton);

            IServiceContainer child = Container.CreateChild();
            child.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            using (IInjector injector = child.CreateInjector())
            {
                Assert.Throws<ServiceNotFoundException>(() => injector.Get<IInterface_2>());
            }
        }

        [Test]
        public void Lifetime_SingletonService_ShouldResolveDependencyFromTheDeclaringContainer_DecorationTest()
        {
            Config.Value.Injector.StrictDI = false;

            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Singleton);

            IServiceContainer child = Container.CreateChild();
            child.Proxy<IInterface_1>((i, curr) => new DecoratedImplementation_1());

            using (IInjector injector = child.CreateInjector())
            {
                Assert.That(injector.Get<IInterface_2>().Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
                Assert.That(injector.Get<IInterface_1>(), Is.InstanceOf<DecoratedImplementation_1>());
            }
        }

        [Test]
        public void Lifetime_PooledService_ShouldBeInstantiatedUpToNTimesPerDeclaringContainer([Values(1, 2, 3)] int times)
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Pooled.WithCapacity(times));

            IInjector[] injectors = Enumerable.Repeat(0, times).Select(_ => 
            {
                IInjector injector = Container.CreateInjector();
                Assert.AreSame(injector.Get<IInterface_1>(), injector.Get<IInterface_1>());
                return injector;
            }).ToArray();

            Assert.False
            (
                Task.Run(() =>
                {
                    using (IInjector injector = Container.CreateInjector())
                    {
                        injector.Get<IInterface_1>();
                    }
                }).Wait(10)
            );

            injectors.Last().Dispose();

            Assert.True
            (
                Task.Run(() =>
                {
                    using (IInjector injector = Container.CreateInjector())
                    {
                        injector.Get<IInterface_1>();
                    }
                }).Wait(10)
            );
        }

        [Test]
        public void Lifetime_PooledService_ShouldHaveItsOwnInjector()
        {
            Assert.Ignore();

            Container
                .Service<IInterface_7<IInjector>, Implementation_7_TInterface_Dependant<IInjector>>(Lifetime.Pooled)
                .Service<IInterface_7<IInjector>, Implementation_7_TInterface_Dependant<IInjector>>("named", Lifetime.Pooled);

            using (IInjector injector = Container.CreateInjector())
            {
                IInterface_7<IInjector> svc = injector.Get<IInterface_7<IInjector>>("named");

                Assert.That(svc.Interface, Is.Not.SameAs(injector));
                Assert.That(svc.Interface.UnderlyingContainer.Parent, Is.SameAs(Container));

                Assert.That(svc.Interface.Get<IInterface_7<IInjector>>().Interface, Is.Not.SameAs(injector));
                Assert.That(svc.Interface.Get<IInterface_7<IInjector>>().Interface.UnderlyingContainer.Parent, Is.SameAs(Container));

                Assert.That(svc.Interface.Get<IInterface_7<IInjector>>().Interface, Is.Not.SameAs(svc.Interface));
            }
        }

        private interface IDisposableService : IDisposableEx { }

        private class DisposableService : Disposable, IDisposableService { }

        [Test]
        public void Lifetime_PooledService_ShouldBeDisposedOnContainerDisposal()
        {
            IDisposableEx disposable;

            using (IServiceContainer container = Container.CreateChild())
            {
                container.Service<IDisposableService, DisposableService>(Lifetime.Pooled);

                using (IInjector injector = container.CreateInjector())
                {
                    disposable = injector.Get<IDisposableService>();
                }

                Assert.False(disposable.Disposed);
            }

            Assert.That(disposable.Disposed);
        }

        [Test]
        public void Lifetime_PooledService_MayHavePooledDependency()
        {
            Container
                .Service<IInterface_1, Implementation_1>(Lifetime.Pooled)
                .Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(Lifetime.Pooled);

            using (IInjector injector = Container.CreateInjector())
            {
                IInterface_7<IInterface_1> svc = injector.Get<IInterface_7<IInterface_1>>();

                Assert.That(svc.Interface, Is.Not.Null);
            }
        }

        [Test]
        public void Lifetime_PooledService_MayHaveRegularDependency()
        {
            Container
                .Service<IInterface_1, Implementation_1>(Lifetime.Transient)
                .Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(Lifetime.Pooled);

            using (IInjector injector = Container.CreateInjector())
            {
                IInterface_7<IInterface_1> svc = injector.Get<IInterface_7<IInterface_1>>();

                Assert.That(svc.Interface, Is.Not.Null);
            }
        }

        private class DisposableServiceHavingDisposableDependency : Disposable, IDisposableService 
        {
            public IDisposableEx DisposableDep { get; }

            public DisposableServiceHavingDisposableDependency(IDisposableEx disposableDep) => DisposableDep = disposableDep;

            protected override void Dispose(bool disposeManaged)
            {
                Assert.False(DisposableDep.Disposed);
            }
        }

        [Test]
        public void Lifetime_PooledService_CanAccessItsDependencyOnDispose([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IDisposableService svc;

            using (IServiceContainer container = Container.CreateChild())
            {
                container
                    .Service<IDisposableEx, Disposable>(lifetime)
                    .Service<IDisposableService, DisposableServiceHavingDisposableDependency>(Lifetime.Pooled);

                using (IInjector injector = container.CreateInjector())
                {
                    svc = injector.Get<IDisposableService>();
                }
            }

            Assert.That(svc.Disposed);
        }

        [Test]
        public void Lifetime_Instance_ShouldBeResolvedFromTheDeclaringContainer() 
        {
            Container.Instance<IDisposable>(new Disposable(), true);

            using (IServiceContainer child = Container.CreateChild())
            {
                IInjector
                    injector1 = Container.CreateInjector(),
                    injector2 = child.CreateInjector();

                Assert.AreSame(injector1.UnderlyingContainer.Get<IDisposable>(), injector2.UnderlyingContainer.Get<IDisposable>());
                Assert.AreSame(injector1.Get<IDisposable>(), injector2.Get<IDisposable>());
            }
        }

        [Test]
        public void Lifetime_PermissiveDI_LegalCases(
            [Values(true, false)] bool useChildContainer,
            [ValueSource(nameof(Lifetimes))] Lifetime dependant,
            [ValueSource(nameof(Lifetimes))] Lifetime dependency)
        {
            Config.Value.Injector.StrictDI = false;

            Container.Service<IInterface_1, Implementation_1_No_Dep>(dependency);

            IServiceContainer sourceContainer = useChildContainer ? Container.CreateChild() : Container;

            sourceContainer.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(dependant);

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = sourceContainer.CreateInjector())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_PermissiveDI_LegalCases(
            [Values(true, false)] bool useChildContainer,
            [ValueSource(nameof(Lifetimes))] Lifetime dependant)
        {
            Config.Value.Injector.StrictDI = false;

            Container.Instance<IInterface_1>(new Implementation_1_No_Dep());

            IServiceContainer sourceContainer = useChildContainer ? Container.CreateChild() : Container;

            sourceContainer.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(dependant);

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = sourceContainer.CreateInjector())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_LegalCases1(
            [Values(true, false)] bool useChildContainer,
            [ValueSource(nameof(InjectorControlledLifetimes))] Lifetime dependant,
            [ValueSource(nameof(Lifetimes))] Lifetime dependency) 
        {
            Config.Value.Injector.StrictDI = true;

            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(dependency)
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(dependant);

            IServiceContainer sourceContainer = useChildContainer ? Container.CreateChild() : Container;

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = sourceContainer.CreateInjector())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_LegalCases1(
            [Values(true, false)] bool useChildContainer,
            [ValueSource(nameof(InjectorControlledLifetimes))] Lifetime dependant)
        {
            Config.Value.Injector.StrictDI = true;

            Container
                .Instance<IInterface_1>(new Implementation_1_No_Dep())
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(dependant);

            IServiceContainer sourceContainer = useChildContainer ? Container.CreateChild() : Container;

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = sourceContainer.CreateInjector())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_LegalCases2([Values(true, false)] bool useChildContainer)
        {
            Config.Value.Injector.StrictDI = true;

            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Singleton)
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Singleton);

            IServiceContainer sourceContainer = useChildContainer ? Container.CreateChild() : Container;

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = sourceContainer.CreateInjector())
                {
                    Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
                }
            }
        }

        [Test]
        public void Lifetime_StrictDI_IllegalCases(
            [Values(true, false)] bool useChildContainer,
            [ValueSource(nameof(InjectorControlledLifetimes))] Lifetime dependency) 
        {
            Config.Value.Injector.StrictDI = true;

            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(dependency)
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Singleton);

            IServiceContainer sourceContainer = useChildContainer ? Container.CreateChild() : Container;

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = sourceContainer.CreateInjector())
                {
                    Assert.Throws<RequestNotAllowedException>(() => injector.Get<IInterface_2>());
                }
            }
        }
    }
}
