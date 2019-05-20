/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void Injector_Get_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Injector.Get<Object>(), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Injector.Get(typeof(Object)), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Get_ShouldInstantiate(Lifetime lifetime)
        {
            Injector.Service<IInterface_1, Implementation_1>(lifetime);

            var instance = Injector.Get<IInterface_1>();

            Assert.That(instance, Is.InstanceOf<Implementation_1>());
            (lifetime == Lifetime.Singleton ? Assert.AreSame : ((Action<object, object>) Assert.AreNotSame))(instance, Injector.Get<IInterface_1>());
        }

        [Test]
        public void Injector_Get_ShouldThrowOnNonRegisteredDependency()
        {
            Assert.Throws<NotSupportedException>(() => Injector.Get<IInterface_1>(), string.Format(Resources.DEPENDENCY_NOT_FOUND, typeof(IInterface_1)));         
        }

        [Test]
        public void Injector_Get_ShouldResolveDependencies()
        {
            Injector
                .Service<IInterface_2, Implementation_2>()
                .Service<IInterface_1, Implementation_1>(); // direkt masodikkent szerepel

            var instance = Injector.Get<IInterface_2>();

            Assert.That(instance, Is.InstanceOf<Implementation_2>());
            Assert.That(instance.Interface1, Is.InstanceOf<Implementation_1>());
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Get_ShouldResolveGenericDependencies(Lifetime lifetime)
        {
            Injector
                .Service<IInterface_1, Implementation_1>() // direkt nincs lifetime
                .Service(typeof(IInterface_3<>), typeof(Implementation_3<>)) // direkt nincs lifetime
                .Service(typeof(IInterface_6<>), typeof(Implementation_6<>), lifetime);

            Assert.That(Injector.Entries.Count, Is.EqualTo(4)); // +1 == self

            var instance = Injector.Get<IInterface_6<string>>();
            
            Assert.That(instance, Is.InstanceOf<Implementation_6<string>>());
            Assert.That(instance.Interface3, Is.InstanceOf<Implementation_3<string>>());
            Assert.That(instance.Interface3.Interface1, Is.InstanceOf<Implementation_1>());

            var assert = lifetime == Lifetime.Singleton ? Assert.AreSame : (Action<object, object>) Assert.AreNotSame;

            assert(instance, Injector.Get<IInterface_6<string>>());
            assert(instance.Interface3, Injector.Get<IInterface_6<string>>().Interface3);

            Assert.That(Injector.Entries.Count, Is.EqualTo(6)); // Implementation_3<string>, Implementation_6<string>
        }

        [Test]
        public void Injector_Get_ShouldNotInstantiateGenericType()
        {
            Injector.Service(typeof(IInterface_3<>), typeof(Implementation_3<>));

            Assert.Throws<ArgumentException>(() => Injector.Get(typeof(IInterface_3<>)), Resources.CANT_INSTANTIATE_GENERICS);
        }

        [Test]
        public void Injector_GetByService_ShouldThrowOnCircularReference()
        {
            Injector
                .Service<IInterface_4, Implementation_4_cdep>()
                .Service<IInterface_5, Implementation_5_cdep>();

            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
        }

        [Test]
        public void Injector_GetByFactory_ShouldThrowOnCircularReference()
        {
            Injector
                .Factory<IInterface_4>(injector => new Implementation_4_cdep(injector.Get<IInterface_5>()))
                .Factory<IInterface_5>(injector => new Implementation_5_cdep(Injector.Get<IInterface_4>()));

            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
        }

        [Test]
        public void Injector_GetByInjector_ShouldThrowOnCircularReference()
        {
            Injector
                .Service<IInterface_1, Implementation_7_cdep>()
                .Service<IInterface_4, Implementation_4_cdep>()
                .Service<IInterface_5, Implementation_5_cdep>();

            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
        }

        [Test]
        public void Injector_GetByProxy_ShouldThrowOnCircularReference()
        {
            Injector
                .Service<IInterface_1, Implementation_1>()
                .Proxy<IInterface_1>((injector, inst) => injector.Get<IInterface_1>());

            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
        }

        [Test]
        public void Injector_Get_ShouldResolveItself()
        {
            Assert.AreSame(Injector, Injector.Get<IInjector>());
        }
    }
}
