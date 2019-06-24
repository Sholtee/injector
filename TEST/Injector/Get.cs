/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
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
        [TestCase(Lifetime.Singleton)]
        public void Injector_Get_ShouldInstantiate(Lifetime lifetime)
        {
            Container.Service<IInterface_1, Implementation_1>(lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_1>();

                Assert.That(instance, Is.InstanceOf<Implementation_1>());
                (lifetime == Lifetime.Singleton ? Assert.AreSame : ((Action<object, object>)Assert.AreNotSame))(instance, injector.Get<IInterface_1>());
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnNonRegisteredDependency()
        {
            Assert.Throws<NotSupportedException>(() => Container.CreateInjector().Get<IInterface_1>(), string.Format(Resources.DEPENDENCY_NOT_FOUND, typeof(IInterface_1)));         
        }

        [Test]
        public void Injector_Get_ShouldResolveDependencies()
        {
            Container
                .Service<IInterface_2, Implementation_2>()
                .Service<IInterface_1, Implementation_1>(); // direkt masodikkent szerepel

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_2>();

                Assert.That(instance, Is.InstanceOf<Implementation_2>());
                Assert.That(instance.Interface1, Is.InstanceOf<Implementation_1>());
            }
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Get_ShouldResolveGenericDependencies(Lifetime lifetime)
        {
            Container
                .Service<IInterface_1, Implementation_1>() // direkt nincs lifetime
                .Service(typeof(IInterface_3<>), typeof(Implementation_3<>)) // direkt nincs lifetime
                .Service(typeof(IInterface_6<>), typeof(Implementation_6<>), lifetime);

            Assert.That(Container.Entries.Count, Is.EqualTo(3));

            using (IInjector injector = Container.CreateInjector())
            {         
                var instance = injector.Get<IInterface_6<string>>();
            
                Assert.That(instance, Is.InstanceOf<Implementation_6<string>>());
                Assert.That(instance.Interface3, Is.InstanceOf<Implementation_3<string>>());
                Assert.That(instance.Interface3.Interface1, Is.InstanceOf<Implementation_1>());

                var assert = lifetime == Lifetime.Singleton ? Assert.AreSame : (Action<object, object>) Assert.AreNotSame;

                assert(instance, injector.Get<IInterface_6<string>>());
                assert(instance.Interface3, injector.Get<IInterface_6<string>>().Interface3);

                Assert.DoesNotThrow(() => injector.QueryServiceInfo<Implementation_3<string>>());
                Assert.DoesNotThrow(() => injector.QueryServiceInfo<Implementation_6<string>>());
            }
        }

        [Test]
        public void Injector_Get_ShouldNotInstantiateGenericType()
        {
            Container.Service(typeof(IInterface_3<>), typeof(Implementation_3<>));

            Assert.Throws<ArgumentException>(() => Container.CreateInjector().Get(typeof(IInterface_3<>)), Resources.CANT_INSTANTIATE_GENERICS);
        }

        [Test]
        public void Injector_GetByService_ShouldThrowOnCircularReference()
        {
            Container
                .Service<IInterface_4, Implementation_4_cdep>()
                .Service<IInterface_5, Implementation_5_cdep>();

            using (IInjector injector = Container.CreateInjector())
            {     
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByFactory_ShouldThrowOnCircularReference()
        {
            Container
                .Factory<IInterface_4>(injector => new Implementation_4_cdep(injector.Get<IInterface_5>()))
                .Factory<IInterface_5>(injector => new Implementation_5_cdep(injector.Get<IInterface_4>()));

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByInjector_ShouldThrowOnCircularReference()
        {
            Container
                .Service<IInterface_1, Implementation_7_cdep>()
                .Service<IInterface_4, Implementation_4_cdep>()
                .Service<IInterface_5, Implementation_5_cdep>();

            Assert.Throws<InvalidOperationException>(() => Container.CreateInjector().Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
        }

        [Test]
        public void Injector_GetByProxy_ShouldThrowOnCircularReference()
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Proxy<IInterface_1>((injector, inst) => injector.Get<IInterface_1>());

            Assert.Throws<InvalidOperationException>(() => Container.CreateInjector().Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
        }

        [Test]
        public void Injector_Get_ShouldResolveItself()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                Assert.AreSame(injector, injector.Get<IInjector>());
            }         
        }
    }
}
