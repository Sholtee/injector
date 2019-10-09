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
                Assert.Throws<ServiceNotFoundException>(() => injector.LifetimeOf<IInterface_3<int>>());
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
