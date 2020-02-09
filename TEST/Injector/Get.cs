/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

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
                Assert.Throws<ArgumentException>(() => injector.Get<Object>(), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
                Assert.Throws<ArgumentException>(() => injector.Get(typeof(Object)), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
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

        [Test]
        public void Injector_Get_ShouldThrowOnNonRegisteredDependency1([Values(true, false)] bool useChildContainer)
        {
            IServiceContainer container = useChildContainer ? Container.CreateChild() : Container;

            using (IInjector injector = container.CreateInjector())
            {
                Assert.Throws<ServiceNotFoundException>(() => injector.Get<IInterface_1>());
            }                 
        }

        [Test]
        public void Injector_Get_ShouldThrowOnNonRegisteredDependency2([Values(true, false)] bool useChildContainer, [Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime)
        {
            Container.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime);

            IServiceContainer container = useChildContainer ? Container.CreateChild() : Container;

            using (IInjector injector = container.CreateInjector())
            {
                Assert.Throws<ServiceNotFoundException>(() => injector.Get<IInterface_7<IInterface_1>>());
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
                Assert.Throws<ArgumentException>(() => injector.Get(typeof(IInterface_3<>)), Resources.CANT_INSTANTIATE_GENERICS);
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
                Assert.Throws<Exception>(() => injector.Get<IInterface_1>(), string.Format(Resources.INVALID_INSTANCE, typeof(IInterface_1)));
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnAbstractService1() 
        {
            using (IInjector injector = Container.CreateInjector()) 
            {
                //
                // Ez az injector megeroszakolasa u h nagyon nem eletszeru.
                //

                injector.UnderlyingContainer.Abstract<IInterface_1>();

                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_1>(), Resources.CANT_INSTANTIATE_ABSTRACTS);
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnAbstractService2()
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
        public void Injector_Get_ShouldDisposeTheUnusedServiceReference() 
        {
            var entry = new HackyServiceEntry();
            Container.Add(entry);

            using (IInjector injector = Container.CreateInjector()) 
            {
                injector.Get<IInterface_1>();

                Assert.That(entry.GotReference.Disposed);
            }
        }

        private sealed class HackyServiceEntry : AbstractServiceEntry
        {
            private AbstractServiceReference FService;

            public AbstractServiceReference GotReference { get; private set; }

            public HackyServiceEntry() : base(typeof(IInterface_1), null)
            {
            }

            public override AbstractServiceReference Instance => FService;

            public override bool SetInstance(AbstractServiceReference serviceReference)
            {
                GotReference = serviceReference;
                FService = new ServiceReference(this) 
                { 
                    Value = new Implementation_1_No_Dep()
                };
                return false;
            }
        }
    }
}
