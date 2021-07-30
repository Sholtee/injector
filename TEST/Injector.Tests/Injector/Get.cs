/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;
    using Internals;
    using Properties;

    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Injector_Get_ShouldThrowOnNonInterfaceKey()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<ArgumentException>(() => injector.Get<Object>(), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
                Assert.Throws<ArgumentException>(() => injector.Get(typeof(Object)), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldInstantiate(Lifetime lifetime)
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_1>();

                Assert.That(instance, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldInstantiateEnumerables(Lifetime lifetime)
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                var enumerable = injector.Get<IEnumerable<IInterface_1>>();

                Assert.That(enumerable.Count(), Is.EqualTo(1));
                Assert.That(enumerable.Single(), Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnNonRegisteredDependency([Values(true, false)] bool useChildContainer, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Container.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime);

            IServiceContainer container = useChildContainer ? Container.CreateChild() : Container;

            using (IInjector injector = container.CreateInjector())
            {
                var e = Assert.Throws<ServiceNotFoundException>(() => injector.Get<IInterface_7<IInterface_1>>());
                Assert.That(e.Data.Contains("path"));
                Assert.That(e.Data["path"], Is.EqualTo(string.Join(" -> ", new ServiceId(typeof(IInterface_7<IInterface_1>), null).FriendlyName(), new ServiceId(typeof(IInterface_1), null).FriendlyName())));
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowOnNonRegisteredDependencyInCaseOfEnumerables()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                var enumerable = injector.Get<IEnumerable<IInterface_1>>();

                Assert.That(enumerable.Any(), Is.False);
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Container
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(lifetime1)
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime2); // direkt masodikkent szerepel

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_2>();

                Assert.That(instance, Is.InstanceOf<Implementation_2_IInterface_1_Dependant>());
                Assert.That(instance.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveLazyDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime1)
                .Service<IInterface_2_LazyDep, Implementation_2_Lazy__IInterface_1_Dependant>(lifetime2);

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_2_LazyDep>();

                Assert.That(instance, Is.InstanceOf<Implementation_2_Lazy__IInterface_1_Dependant>());
                Assert.That(instance.Interface1, Is.InstanceOf<Lazy<IInterface_1>>());
                Assert.That(instance.Interface1.Value, Is.InstanceOf<IInterface_1>());
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldResolveGenericDependencies(Lifetime lifetime)
        {
            Config.Value.Injector.StrictDI = false;

            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), Lifetime.Transient)
                .Service(typeof(IInterface_6<>), typeof(Implementation_6_IInterface_3_Dependant<>), lifetime);

            using (IInjector injector = Container.CreateInjector())
            {         
                var instance = injector.Get<IInterface_6<string>>();
            
                Assert.That(instance, Is.InstanceOf<Implementation_6_IInterface_3_Dependant<string>>());
                Assert.That(instance.Interface3, Is.InstanceOf<Implementation_3_IInterface_1_Dependant<string>>());
                Assert.That(instance.Interface3.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldThrowOnOpenGenericType(Lifetime lifetime)
        {
            Container.Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<ArgumentException>(() => injector.Get(typeof(IInterface_3<>)), Resources.PARAMETER_IS_GENERIC);
            }          
        }

        [Test]
        public void Injector_Get_ShouldThrowOnNull()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<ArgumentNullException>(() => injector.Get(null));
            }
        }

        private interface IMyService 
        {
            void DoSomething();
        }

        private class ImplementationHavingInlineDep : IMyService
        {
            public IInjector Injector { get; }

            public ImplementationHavingInlineDep(IInjector injector) => Injector = injector;

            public void DoSomething() => Injector.Get<IInterface_1>();
        }

        [Test]
        public void Injector_Get_ShouldWorkInline([ValueSource(nameof(Lifetimes))] Lifetime requestorLifetime, [ValueSource(nameof(Lifetimes))] Lifetime depLifetime) 
        {
            Container
                .Service<IInterface_1, Implementation_1>(depLifetime)
                .Service<IMyService, ImplementationHavingInlineDep>(requestorLifetime)
                .Service<IInterface_7<IMyService>, Implementation_7_TInterface_Dependant<IMyService>>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                IMyService svc = injector.Get<IInterface_7<IMyService>>().Interface;

                Assert.DoesNotThrow(svc.DoSomething);
            }
        }

        [Test]
        public void Injector_GetByProvider_ShouldThrowOnCircularReference()
        {
            Container
                .Service<IInterface_4, Implementation_4_CDep>(Lifetime.Transient)
                .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Transient)
                .Provider<IInterface_1, CdepProvider>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            }
        }

        private sealed class CdepProvider : IServiceProvider 
        {
            public CdepProvider(IInterface_4 dep) { }
            public object GetService(Type serviceType) => throw new NotImplementedException();
        }

        [Test]
        public void Injector_GetByService_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Container
                .Service<IInterface_4, Implementation_4_CDep>(lifetime1)
                .Service<IInterface_5, Implementation_5_CDep>(lifetime2);

            using (IInjector injector = Container.CreateInjector())
            {     
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByFactory_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Container
                .Factory<IInterface_4>(injector => new Implementation_4_CDep(injector.Get<IInterface_5>()), lifetime1)
                .Factory<IInterface_5>(injector => new Implementation_5_CDep(injector.Get<IInterface_4>()), lifetime2);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByCtor_ShouldThrowOnCircularReference()
        {
            //
            // IInjector.Get() hivasok a konstruktorban vannak
            //

            Container
                .Service<IInterface_1, Implementation_7_CDep>(Lifetime.Transient)
                .Service<IInterface_4, Implementation_4_CDep>(Lifetime.Transient)
                .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_GetByProxy_ShouldThrowOnCircularReference(Lifetime lifetime)
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime)
                .Proxy<IInterface_1>((injector, inst) => injector.Get<IInterface_1>());

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnRecursiveReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            Container.Service<IInterface_1, Implementation_10_RecursiveCDep>(lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveItself()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                Assert.AreSame(injector, injector.Get<IInjector>());
            }         
        }

        [Test]
        public void Injector_Get_ShouldBeTypeChecked() 
        {
            Container.Factory(typeof(IInterface_1), (injector, iface) => new object(), Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector()) 
            {
                Assert.Throws<InvalidCastException>(() => injector.Get<IInterface_1>(), string.Format(Resources.INVALID_INSTANCE, typeof(IInterface_1)));
                Assert.That(injector.UnderlyingContainer.Get<IInterface_1>().Instances, Is.Empty);
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnAbstractService()
        {
            Container
                .Abstract<IInterface_1>()
                .Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(Lifetime.Singleton);

            //
            // Felulirjuk az absztrakt szervizt h letre tudjunk hozni injectort.
            //

            IServiceContainer child = Container
                .CreateChild()
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            //
            // Mivel a singleton szerviz fuggosegei is a deklaralo kontenerbol lesznek feloldva ezert
            // o meg siman tud hivatkozni absztrakt szervizre.
            //

            using (IInjector injector = child.CreateInjector())
            {
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_7<IInterface_1>>(), Resources.INVALID_INJECTOR_ENTRY);
            }
        }

        [Test]
        public void Injector_Get_ShouldDisposeTheServiceReferenceOnError()
        {
            var entry = new HackyServiceEntry(Container);
            Container.Add(entry);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<Exception>(() => injector.Get<IInterface_1>());

                Assert.That(entry.GotReference.RefCount == 0);
            }
        }

        private sealed class HackyServiceEntry : AbstractServiceEntry
        {
            public IServiceReference GotReference { get; private set; }

            public HackyServiceEntry(IServiceContainer owner) : base(typeof(IInterface_1), null, owner) { }

            public override bool SetInstance(IServiceReference serviceReference)
            {
                GotReference = serviceReference;
                
                throw new Exception();
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowIfAMissingDependencyIsOptional() 
        {
            Container.Service<IInterface_7<IInterface_1>, Implementation_7_UsingOptionalDependency>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector()) 
            {
                IInterface_7<IInterface_1> svc = null;
                
                Assert.DoesNotThrow(() => svc = injector.Get<IInterface_7<IInterface_1>>());
                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.Null);
            }

            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.That(injector.Get<IInterface_7<IInterface_1>>().Interface, Is.Not.Null);
            }
        }

        private sealed class Implementation_7_UsingOptionalDependency : Implementation_7_TInterface_Dependant<IInterface_1>
        {
            public Implementation_7_UsingOptionalDependency([Options(Optional = true)] IInterface_1 dep) : base(dep)
            {
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowIfAMissingLazyDependencyIsOptional()
        {
            Container.Service<IInterface_7<Lazy<IInterface_1>>, Implementation_7_UsingOptionalLazyDependency>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                IInterface_7<Lazy<IInterface_1>> svc = null;

                Assert.DoesNotThrow(() => svc = injector.Get<IInterface_7<Lazy<IInterface_1>>>());
                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.Not.Null);
                Assert.That(svc.Interface.Value, Is.Null);
            }

            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.That(injector.Get<IInterface_7<Lazy<IInterface_1>>>().Interface.Value, Is.Not.Null);
            }
        }

        private sealed class Implementation_7_UsingOptionalLazyDependency : Implementation_7_TInterface_Dependant<Lazy<IInterface_1>>
        {
            public Implementation_7_UsingOptionalLazyDependency([Options(Optional = true)] Lazy<IInterface_1> dep) : base(dep)
            {
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldThrowIfTheServiceIsNotProducible(Lifetime lifetime) 
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime);

            var setter = (ISupportsProxying) Container.Get<IInterface_1>();
            setter.Factory = null;

            using (IInjector injector = Container.CreateInjector()) 
            {
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_1>(), Resources.NOT_PRODUCIBLE);
            }
        }

        [Test]
        public void Injector_Get_ClosedGenericsShouldHaveThePriorityOverTheOpenOnes()
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Service(typeof(IInterface_3<>), typeof(NotUsedImplementation<>), Lifetime.Transient)
                .Service<IInterface_3<int>, Implementation_3_IInterface_1_Dependant<int>>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                IInterface_3<int> svc = injector.Get<IInterface_3<int>>();

                Assert.That(svc, Is.InstanceOf<Implementation_3_IInterface_1_Dependant<int>>());
            }
        }

        private sealed class NotUsedImplementation<T> : IInterface_3<T>
        {
            public IInterface_1 Interface1 => throw new NotImplementedException();
        }
    }
}
