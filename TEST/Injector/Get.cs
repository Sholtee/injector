/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
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

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Get_ShouldInstantiate(Lifetime lifetime)
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_1>();

                Assert.That(instance, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Singleton)]
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
        public void Injector_Get_ShouldThrowOnNonRegisteredDependency([Values(true, false)] bool useChildContainer, [Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime)
        {
            Container.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime);

            IServiceContainer container = useChildContainer ? Container.CreateChild() : Container;

            using (IInjector injector = container.CreateInjector())
            {
                var e = Assert.Throws<ServiceNotFoundException>(() => injector.Get<IInterface_7<IInterface_1>>());
                Assert.That(e.Data.Contains("path"));
                Assert.That(e.Data["path"], Is.EqualTo(string.Join(" -> ", typeof(IInterface_7<IInterface_1>), typeof(IInterface_1))));
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
        public void Injector_Get_ShouldResolveDependencies()
        {
            Container
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>()
                .Service<IInterface_1, Implementation_1_No_Dep>(); // direkt masodikkent szerepel

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_2>();

                Assert.That(instance, Is.InstanceOf<Implementation_2_IInterface_1_Dependant>());
                Assert.That(instance.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveLazyDependencies()
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .Service<IInterface_2_LazyDep, Implementation_2_Lazy__IInterface_1_Dependant>();

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_2_LazyDep>();

                Assert.That(instance, Is.InstanceOf<Implementation_2_Lazy__IInterface_1_Dependant>());
                Assert.That(instance.Interface1, Is.InstanceOf<Lazy<IInterface_1>>());
                Assert.That(instance.Interface1.Value, Is.InstanceOf<IInterface_1>());
            }
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Get_ShouldResolveGenericDependencies(Lifetime lifetime)
        {
            Config.Value.Injector.StrictDI = false;

            Container
                .Service<IInterface_1, Implementation_1_No_Dep>() // direkt nincs lifetime
                .Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>)) // direkt nincs lifetime
                .Service(typeof(IInterface_6<>), typeof(Implementation_6_IInterface_3_Dependant<>), lifetime);

            Assert.That(Container.Count, Is.EqualTo(3));

            using (IInjector injector = Container.CreateInjector())
            {         
                var instance = injector.Get<IInterface_6<string>>();
            
                Assert.That(instance, Is.InstanceOf<Implementation_6_IInterface_3_Dependant<string>>());
                Assert.That(instance.Interface3, Is.InstanceOf<Implementation_3_IInterface_1_Dependant<string>>());
                Assert.That(instance.Interface3.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnOpenGenericType()
        {
            Container.Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>));

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

        [Test]
        public void Injector_GetByService_ShouldThrowOnCircularReference()
        {
            Container
                .Service<IInterface_4, Implementation_4_CDep>()
                .Service<IInterface_5, Implementation_5_CDep>();

            using (IInjector injector = Container.CreateInjector())
            {     
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByFactory_ShouldThrowOnCircularReference()
        {
            Container
                .Factory<IInterface_4>(injector => new Implementation_4_CDep(injector.Get<IInterface_5>()))
                .Factory<IInterface_5>(injector => new Implementation_5_CDep(injector.Get<IInterface_4>()));

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByInjector_ShouldThrowOnCircularReference()
        {
            Container
                .Service<IInterface_1, Implementation_7_CDep>()
                .Service<IInterface_4, Implementation_4_CDep>()
                .Service<IInterface_5, Implementation_5_CDep>();

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            }
        }

        [Test]
        public void Injector_GetByProxy_ShouldThrowOnCircularReference()
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .Proxy<IInterface_1>((injector, inst) => injector.Get<IInterface_1>());

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnCircularReferenceEvenIfTheServicesHaveDifferentLifetime([Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime) 
        {
            Container
                .Service<IInterface_4, Implementation_4_CDep>(lifetime)
                .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Singleton); // mindig kulon injectort kap

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnRecursiveReference([Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime) 
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
            Container.Factory(typeof(IInterface_1), (injector, iface) => new object());

            using (IInjector injector = Container.CreateInjector()) 
            {
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_1>(), string.Format(Resources.INVALID_INSTANCE, typeof(IInterface_1)));
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
                .Service<IInterface_1, Implementation_1_No_Dep>();

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

                Assert.That(entry.GotReference.Disposed);
            }
        }

        private sealed class HackyServiceEntry : AbstractServiceEntry
        {
            public ServiceReference GotReference { get; private set; }

            public HackyServiceEntry(IServiceContainer owner) : base(typeof(IInterface_1), null, null, owner) { }

            public override bool SetInstance(ServiceReference serviceReference, IReadOnlyDictionary<string, object> options)
            {
                GotReference = serviceReference;
                
                throw new Exception();
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowIfAMissingDependencyIsOptional() 
        {
            Container.Service<IInterface_7<IInterface_1>, Implementation_7_UsingOptionalDependency>();

            using (IInjector injector = Container.CreateInjector()) 
            {
                IInterface_7<IInterface_1> svc = null;
                
                Assert.DoesNotThrow(() => svc = injector.Get<IInterface_7<IInterface_1>>());
                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.Null);
            }

            Container.Service<IInterface_1, Implementation_1_No_Dep>();

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
            Container.Service<IInterface_7<Lazy<IInterface_1>>, Implementation_7_UsingOptionalLazyDependency>();

            using (IInjector injector = Container.CreateInjector())
            {
                IInterface_7<Lazy<IInterface_1>> svc = null;

                Assert.DoesNotThrow(() => svc = injector.Get<IInterface_7<Lazy<IInterface_1>>>());
                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.Not.Null);
                Assert.That(svc.Interface.Value, Is.Null);
            }

            Container.Service<IInterface_1, Implementation_1_No_Dep>();

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

        [Test]
        public void Injector_Get_ShouldThrowIfTheServiceIsNotProducible([Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime) 
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime);

            using (IInjector injector = Container.CreateInjector()) 
            {
                var setter = (ISupportsProxying) injector.UnderlyingContainer.Get<IInterface_1>();
                setter.Factory = null;

                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_1>(), Resources.NOT_PRODUCIBLE);
            }
        }

        [Test]
        public void Injector_Get_ClosedGenericsShouldHaveThePriorityOverTheOpenOnes()
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .Service(typeof(IInterface_3<>), typeof(NotUsedImplementation<>))
                .Service<IInterface_3<int>, Implementation_3_IInterface_1_Dependant<int>>();

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
