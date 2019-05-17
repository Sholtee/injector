using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void Injector_Proxy_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Injector.Proxy<Object>((p1, p2) => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Injector.Proxy(typeof(Object), (p1, p2, p3) => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Proxy_ShouldOverwriteTheFactoryFunction(Lifetime lifetime)
        {
            int
                callCount_1 = 0,
                callCount_2 = 0;

            Injector
                .Service<IInterface_1, Implementation_1>(lifetime)
                .Proxy(typeof(IInterface_1), (injector, t, inst) =>
                {
                    Assert.AreSame(injector, Injector);
                    Assert.That(t, Is.EqualTo(typeof(IInterface_1)));
                    Assert.That(inst, Is.InstanceOf<Implementation_1>());

                    callCount_1++;
                    return inst;
                })
                .Proxy<IInterface_1>((injector, inst) =>
                {
                    Assert.AreSame(injector, Injector);
                    Assert.That(inst, Is.TypeOf<Implementation_1>());

                    callCount_2++;
                    return new DecoratedImplementation_1();
                });

            var instance = Injector.Get<IInterface_1>();
            
            Assert.That(instance, Is.InstanceOf<DecoratedImplementation_1>());
            Assert.That(callCount_1, Is.EqualTo(1));
            Assert.That(callCount_2, Is.EqualTo(1));

            (lifetime == Lifetime.Singleton ? Assert.AreSame : ((Action<object, object>) Assert.AreNotSame))(instance, Injector.Get<IInterface_1>());
        }

        [Test]
        public void Injector_Proxy_ShouldWorkWithGenericTypes()
        {
            int callCount = 0;

            Injector
                .Service<IInterface_1, Implementation_1>()
                .Service(typeof(IInterface_3<>), typeof(Implementation_3<>))
                .Proxy(typeof(IInterface_3<int>), (injector, type, inst) =>
                {
                    Assert.AreSame(injector, Injector);
                    Assert.AreSame(type, typeof(IInterface_3<int>));
                    Assert.That(inst, Is.InstanceOf<Implementation_3<int>>());

                    callCount++;
                    return new DecoratedImplementation_3<int>();
                });

            var instance = Injector.Get<IInterface_3<int>>();
            
            Assert.That(instance, Is.InstanceOf<DecoratedImplementation_3<int>>());
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Injector_Proxy_ShouldThrowOnOpenGenericParameter()
        {
            Injector.Service(typeof(IInterface_3<>), typeof(Implementation_3<>));

            Assert.Throws<InvalidOperationException>(() => Injector.Proxy(typeof(IInterface_3<>), (injector, type, inst) => inst), Resources.CANT_PROXY);
        }

        [Test]
        public void Injector_Proxy_ShouldBeTypeChecked()
        {
            Injector
                .Service<IInterface_1, Implementation_1>()
                .Proxy(typeof(IInterface_1), (injector, type, inst) => new object());

            Assert.Throws<Exception>(() => Injector.Get<IInterface_1>(), string.Format(Resources.INVALID_TYPE, typeof(IInterface_1)));
        }

        [Test]
        public void Injector_Proxy_ShouldThrowOnInstances()
        {
            Injector.Instance<IInterface_1>(new Implementation_1());

            Assert.Throws<InvalidOperationException>(() => Injector.Proxy<IInterface_1>((p1, p2) => null), Resources.CANT_PROXY);
        }
    }
}
