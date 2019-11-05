/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Properties;
    using Internals;

    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Lifetime_TransientService_ShouldBeInstantiatedOnEveryRequest()
        {
            Container.Service<IInterface_1, Implementation_1>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.AreNotSame(injector.Get<IInterface_1>(), injector.Get<IInterface_1>());
            }
        }

        [Test]
        public void Lifetime_InheritedTransientService_ShouldBeInstantiatedOnEveryRequest()
        {
            Container.Service<IInterface_1, Implementation_1>(Lifetime.Transient);

            using (IInjector injector = Container.CreateChild().CreateInjector())
            {
                Assert.AreNotSame(injector.Get<IInterface_1>(), injector.Get<IInterface_1>());
            }
        }

        [Test]
        public void Lifetime_ScopedService_ShouldBeInstantiatedOnlyOncePerInjector()
        {
            Container.Service<IInterface_1, Implementation_1>(Lifetime.Scoped);

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
        public void Lifetime_InheritedScopedService_ShouldBeInstantiatedOnlyOncePerInjector()
        {
            IServiceContainer childContainer = Container // nem muszaj dispose-olni Container felszabaditasakor ugy is dispose-olva lesz
                .Service<IInterface_1, Implementation_1>(Lifetime.Scoped)
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
        public void Lifetime_SingletonService_ShouldBeInstantiatedOnlyOncePerContainer()
        {
            Container.Service<IInterface_1, Implementation_1>(Lifetime.Singleton);

            using (IInjector injector1 = Container.CreateInjector())
            {
                Assert.AreSame(injector1.Get<IInterface_1>(), injector1.Get<IInterface_1>());

                using (IInjector injector2 = Container.CreateInjector())
                {
                    Assert.AreSame(injector2.Get<IInterface_1>(), injector2.Get<IInterface_1>());
                    Assert.AreSame(injector1.Get<IInterface_1>(), injector2.Get<IInterface_1>());

                    using (IInjector injector3 = Container.CreateChild().CreateInjector())
                    {                    
                        Assert.AreSame(injector1.Get<IInterface_1>(), injector3.Get<IInterface_1>());
                        Assert.AreSame(injector2.Get<IInterface_1>(), injector3.Get<IInterface_1>());
                        Assert.AreSame(injector3.Get<IInterface_1>(), injector3.Get<IInterface_1>());
                    }
                }
            }
        }

        [Test]
        public void Lifetime_SingletonService_ShouldUseItsDeclaringContainerForDependecyResolution_Decoration() 
        {
            Container
                .Service<IInterface_1, Implementation_1>(Lifetime.Transient)
                .Service<IInterface_2, Implementation_2>(Lifetime.Scoped);

            IServiceContainer child = Container.CreateChild();
            child.Proxy<IInterface_1>((i, curr) => new DecoratedImplementation_1());

            using (IInjector injector = child.CreateInjector()) 
            {
                Assert.That(injector.Get<IInterface_2>().Interface1, Is.InstanceOf<Implementation_1>());
                Assert.That(injector.Get<IInterface_1>(), Is.InstanceOf<DecoratedImplementation_1>());
            }
        }

        [Test]
        public void Lifetime_SingletonService_ShouldUseItsDeclaringContainerForDependecyResolution_Declaration()
        {
            Container.Service<IInterface_2, Implementation_2>(Lifetime.Singleton);

            IServiceContainer child = Container.CreateChild();
            child.Service<IInterface_1, Implementation_1>(Lifetime.Transient);

            using (IInjector injector = child.CreateInjector()) 
            {
                Assert.Throws<ServiceNotFoundException>(() => injector.Get<IInterface_2>());         
            }
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
        public void Lifetime_NonSingletonService_ShouldResolveDependencyFromChildContainer(Lifetime lifetime) 
        {
            Container.Service<IInterface_2, Implementation_2>(lifetime);

            IServiceContainer child = Container.CreateChild();
            child.Service<IInterface_1, Implementation_1>(Lifetime.Transient);

            using (IInjector injector = child.CreateInjector())
            {
                Assert.DoesNotThrow(() => injector.Get<IInterface_2>());
            }
        }

        [Test]
        public void Injector_LifetimeOf_ShouldReturnTheProperLifetime()
        {
            Container
                .Service<IInterface_1, Implementation_1>(Lifetime.Scoped)
                .Service<IInterface_2, Implementation_2>(Lifetime.Singleton)
                .Instance<IDisposable>(new Disposable());

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.That(injector.LifetimeOf<IInterface_1>(), Is.EqualTo(Lifetime.Scoped));
                Assert.That(injector.LifetimeOf<IInterface_2>(), Is.EqualTo(Lifetime.Singleton));
                Assert.That(injector.LifetimeOf<IDisposable>(),  Is.Null);
            }
        }

        [Test]
        public void Injector_LifetimeOf_ShouldNotSpecialize()
        {
            Container.Service(typeof(IInterface_3<>), typeof(Implementation_3<>));

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Null(injector.LifetimeOf<IInterface_3<int>>());
            }
        }

        [Test]
        public void Injector_LifetimeOf_ShouldValidate()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<ArgumentNullException>(() => injector.LifetimeOf(null));
                Assert.Throws<ArgumentException>(() => injector.LifetimeOf<object>(), Resources.NOT_AN_INTERFACE);
            }
        }

        [TestCase(Lifetime.Singleton)]
        [TestCase(Lifetime.Transient)]
        public void Injector_ShouldRelease_ShouldTakeLifetimeIntoAccount(Lifetime lifetime)
        {
            Container.Service<IDisposable, Disposable>(lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.That(injector.ShouldRelease<IDisposable>(), Is.EqualTo(lifetime == Lifetime.Transient));
            }
        }
    }
}
