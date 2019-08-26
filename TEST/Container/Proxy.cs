/********************************************************************************
* Proxy.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Properties;
    using Proxy;

    [TestFixture]
    public sealed partial class ContainerTests
    {
        [Test]
        public void Container_Proxy_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Container.Proxy<Object>((p1, p2) => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Container.Proxy(typeof(Object), (p1, p2, p3) => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
        public void Container_Proxy_ShouldOverwriteTheFactoryFunction(Lifetime lifetime)
        {
            int
                callCount_1 = 0,
                callCount_2 = 0;

            Container
                .Service<IInterface_1, Implementation_1>(lifetime)
                .Proxy(typeof(IInterface_1), (injector, t, inst) =>
                {
                    Assert.That(t, Is.EqualTo(typeof(IInterface_1)));
                    Assert.That(inst, Is.InstanceOf<Implementation_1>());

                    callCount_1++;
                    return inst;
                })
                .Proxy<IInterface_1>((injector, inst) =>
                {
                    Assert.That(inst, Is.TypeOf<Implementation_1>());

                    callCount_2++;
                    return new DecoratedImplementation_1();
                });

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_1>();

                Assert.That(instance, Is.InstanceOf<DecoratedImplementation_1>());
                Assert.That(callCount_1, Is.EqualTo(1));
                Assert.That(callCount_2, Is.EqualTo(1));

                (lifetime == Lifetime.Scoped ? Assert.AreSame : ((Action<object, object>) Assert.AreNotSame))(instance, injector.Get<IInterface_1>());
            }
        }

        [Test]
        public void Container_Proxy_ShouldWorkWithGenericServices()
        {
            int callCount = 0;

            Container
                .Service<IInterface_1, Implementation_1>()
                .Service(typeof(IInterface_3<>), typeof(Implementation_3<>))
                .Proxy(typeof(IInterface_3<int>), (injector, type, inst) =>
                {
                    Assert.AreSame(type, typeof(IInterface_3<int>));
                    Assert.That(inst, Is.InstanceOf<Implementation_3<int>>());

                    callCount++;
                    return new DecoratedImplementation_3<int>();
                });

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_3<int>>();

                Assert.That(instance, Is.InstanceOf<DecoratedImplementation_3<int>>());
                Assert.That(callCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Container_Proxy_ShouldWorkWithLazyServices()
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(typeof(Implementation_1));

            Container
                .Lazy<IInterface_1>(mockResolver.Object)
                .Proxy<IInterface_1>((injector, inst) => new DecoratedImplementation_1());

            //
            // Az elso Get()-eleskor kell hivja a rendszer a resolver-t
            //

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Never);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.That(injector.Get<IInterface_1>(), Is.InstanceOf<DecoratedImplementation_1>());
            }         
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnOpenGenericParameter()
        {
            Container.Service(typeof(IInterface_3<>), typeof(Implementation_3<>));

            Assert.Throws<InvalidOperationException>(() => Container.Proxy(typeof(IInterface_3<>), (injector, type, inst) => inst), Resources.CANT_PROXY);
        }

        [Test]
        public void Container_Proxy_ShouldBeTypeChecked()
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Proxy(typeof(IInterface_1), (injector, type, inst) => new object());

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<Exception>(() => injector.Get<IInterface_1>(), string.Format(Resources.INVALID_INSTANCE, typeof(IInterface_1)));
            }           
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnInstances()
        {
            Container.Instance<IInterface_1>(new Implementation_1());

            Assert.Throws<InvalidOperationException>(() => Container.Proxy<IInterface_1>((p1, p2) => default(IInterface_1)), Resources.CANT_PROXY);
        }
       
        [Test]
        public void Container_Proxy_MayHaveDependency()
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Service<IInterface_2, Implementation_2>()
                .Proxy<IInterface_2, MyProxyWithDependency>();

            using (IInjector injector = Container.CreateInjector())
            {
                IInterface_2 instance = injector.Get<IInterface_2>();

                Assert.That(instance, Is.InstanceOf<MyProxyWithDependency>());

                MyProxyWithDependency implementor = (MyProxyWithDependency) instance;

                Assert.That(implementor.Dependency, Is.InstanceOf<Implementation_1>());
                Assert.That(implementor.Target, Is.InstanceOf<Implementation_2>());
            }
        }

        public class MyProxyWithDependency : InterfaceInterceptor<IInterface_2>
        {
            public MyProxyWithDependency(IInterface_1 dependency, IInterface_2 target) : base(target)
            {
                Dependency = dependency;
            }

            public IInterface_1 Dependency { get; }
        }
    }
}
