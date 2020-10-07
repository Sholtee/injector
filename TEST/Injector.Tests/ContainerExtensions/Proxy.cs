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
    using Interfaces;
    using Properties;

    using Utils.Proxy;
    
    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_Proxy_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceContainerAdvancedExtensions.Proxy(null, typeof(IDisposable), null, typeof(InterfaceInterceptor<IDisposable>)));
            Assert.Throws<ArgumentNullException>(() => Container.Proxy(null, null, typeof(InterfaceInterceptor<IDisposable>)));
            //Assert.Throws<ArgumentNullException>(() => Container.Proxy(typeof(IDisposable), null, (Type) null));

            Assert.Throws<ArgumentNullException>(() => IServiceContainerBasicExtensions.Proxy(null, typeof(IDisposable), null, (i, t, o) => o));
            Assert.Throws<ArgumentNullException>(() => Container.Proxy(null, null, (i, t, o) => o));
            //Assert.Throws<ArgumentNullException>(() => Container.Proxy(typeof(IDisposable), null, (Func<IInjector, Type, object, object>) null));
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Container.Proxy<Object>((p1, p2) => null), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Container.Proxy(typeof(Object), (p1, p2, p3) => null), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
        }

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Container_Proxy_ShouldOverwriteTheFactoryFunction(Lifetime lifetime)
        {
            var mockCallback1 = new Mock<Func<IInjector, IInterface_1, IInterface_1>>(MockBehavior.Strict);
            mockCallback1
                .Setup(_ => _(It.IsAny<IInjector>(), It.Is<IInterface_1>(inst => inst is Implementation_1_No_Dep)))
                .Returns<IInjector, IInterface_1>((injector, inst) => inst);

            var mockCallback2 = new Mock<Func<IInjector, IInterface_1, IInterface_1>>(MockBehavior.Strict);
            mockCallback2
                .Setup(_ => _(It.IsAny<IInjector>(), It.Is<IInterface_1>(inst => inst is Implementation_1_No_Dep)))
                .Returns(new DecoratedImplementation_1());

            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime)
                .Proxy(mockCallback1.Object)
                .Proxy(mockCallback2.Object);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.UnderlyingContainer)
                .Returns(Container);

            ServiceReference svc = new ServiceReference(Container.Get<IInterface_1>(), mockInjector.Object);

            Assert.That(Container.Get<IInterface_1>().SetInstance(svc, FactoryOptions));
            Assert.That(svc.Value, Is.InstanceOf<DecoratedImplementation_1>());

            mockCallback1.Verify(_ => _(It.IsAny<IInjector>(), It.IsAny<IInterface_1>()), Times.Once);
            mockCallback2.Verify(_ => _(It.IsAny<IInjector>(), It.IsAny<IInterface_1>()), Times.Once);
        }

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Container_Proxy_ShouldWorkWithGenericServices(Lifetime lifetime)
        {
            var mockCallback = new Mock<Func<IInjector, IInterface_3<int>, IInterface_3<int>>>(MockBehavior.Strict);
            mockCallback
                .Setup(_ => _(It.IsAny<IInjector>(), It.Is<IInterface_3<int>>(inst => inst is Implementation_3_IInterface_1_Dependant<int>)))
                .Returns(new DecoratedImplementation_3<int>());

            Container
                .Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), lifetime)
                .Proxy(mockCallback.Object);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_1)), null))
                .Returns(new Implementation_1_No_Dep());
            mockInjector
                .SetupGet(i => i.UnderlyingContainer)
                .Returns(Container);

            //
            // Nem kell QueryMode.AllowSpecialization mert a Proxy() hivas mar
            // rogzitette az uj elemet.
            //

            ServiceReference svc = new ServiceReference(Container.Get<IInterface_3<int>>(), mockInjector.Object);

            Assert.That(Container.Get<IInterface_3<int>>().SetInstance(svc, FactoryOptions));
            Assert.That(svc.Value, Is.InstanceOf<DecoratedImplementation_3<int>>());

            mockCallback.Verify(_ => _(It.IsAny<IInjector>(), It.IsAny<IInterface_3<int>>()), Times.Once);
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Proxy_ShouldThrowOnOpenGenericParameter(Lifetime lifetime)
        {
            Container.Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), lifetime);

            Assert.Throws<InvalidOperationException>(() => Container.Proxy(typeof(IInterface_3<>), (injector, type, inst) => inst), Resources.CANT_PROXY);
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnNotOwnedSingletonService()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Singleton);
            Assert.DoesNotThrow(() => Container.Proxy<IInterface_1>((i, c) => c));

            using (IServiceContainer child = Container.CreateChild())
            {
                Assert.Throws<InvalidOperationException>(() => child.Proxy<IInterface_1>((i, c) => c), Resources.CANT_PROXY);
            }
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnInstances()
        {
            Container.Instance<IInterface_1>(new Implementation_1_No_Dep());

            Assert.Throws<InvalidOperationException>(() => Container.Proxy<IInterface_1>((p1, p2) => default), Resources.CANT_PROXY);
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnAbstractService()
        {
            Container.Abstract<IInterface_1>();

            Assert.Throws<InvalidOperationException>(() => Container.Proxy<IInterface_1>((p1, p2) => default), Resources.CANT_PROXY);
        }

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Container_Proxy_MayHaveDependency(Lifetime lifetime)
        {
            Container
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(lifetime)
                .Proxy<IInterface_2, MyProxyWithDependency>();

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_1)), null))
                .Returns(new Implementation_1_No_Dep());

            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_3<int>)), null))
                .Returns(new Implementation_3_IInterface_1_Dependant<int>(null));

            mockInjector
                .SetupGet(i => i.UnderlyingContainer)
                .Returns(Container);

            ServiceReference svc = new ServiceReference(Container.Get<IInterface_2>(), mockInjector.Object);

            Assert.That(Container.Get<IInterface_2>().SetInstance(svc, FactoryOptions));
            Assert.That(svc.Value, Is.InstanceOf<MyProxyWithDependency>());

            var implementor = (MyProxyWithDependency) svc.Value;

            Assert.That(implementor.Dependency, Is.InstanceOf<Implementation_3_IInterface_1_Dependant<int>>());
            Assert.That(implementor.Target, Is.InstanceOf<Implementation_2_IInterface_1_Dependant>());

            var original = (Implementation_2_IInterface_1_Dependant) implementor.Target;

            Assert.That(original.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_1)), null), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_3<int>)), null), Times.Once);
        }

        public class MyProxyWithDependency : InterfaceInterceptor<IInterface_2>
        {
            public MyProxyWithDependency(IInterface_3<int> dependency, IInterface_2 target) : base(target)
            {
                Dependency = dependency;
            }

            public IInterface_3<int> Dependency { get; }
        }

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Container_Proxy_ShouldHandleNamedServices(Lifetime lifetime) 
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>("cica", lifetime);
            Assert.DoesNotThrow(() => Container.Proxy<IInterface_1>("cica", (i, s) => new DecoratedImplementation_1()));

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.UnderlyingContainer)
                .Returns(Container);

            ServiceReference svc = new ServiceReference(Container.Get<IInterface_1>("cica"), mockInjector.Object);

            Assert.That(Container.Get<IInterface_1>("cica").SetInstance(svc, FactoryOptions));
            Assert.That(svc.Value, Is.TypeOf<DecoratedImplementation_1>());
        }
    }
}
