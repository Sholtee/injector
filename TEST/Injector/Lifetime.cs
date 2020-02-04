﻿/********************************************************************************
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
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.AreNotSame(injector.Get<IInterface_1>(), injector.Get<IInterface_1>());
            }
        }

        [Test]
        public void Lifetime_TransientService_ShouldNotBeInstantiatedIfTheInjectorWasRecycled() 
        {
            Config.Value.MaxSpawnedServices = 1;

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
        public void Lifetime_SingletonService_MayHaveScopedDependency()
        {
            Config.Value.StrictDI = false;

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

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
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
            Config.Value.StrictDI = false;

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
    }
}
