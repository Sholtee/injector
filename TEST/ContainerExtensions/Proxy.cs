/********************************************************************************
* Proxy.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Internals;
    using Properties;
    using Proxy;
    using Annotations;
    
    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_Proxy_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Container.Proxy<Object>((p1, p2) => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Container.Proxy(typeof(Object), (p1, p2, p3) => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Singleton)]
        public void Container_Proxy_ShouldOverwriteTheFactoryFunction(Lifetime lifetime)
        {
            var mockCallback1 = new Mock<Func<IInjector, IInterface_1, IInterface_1>>(MockBehavior.Strict);
            mockCallback1
                .Setup(_ => _(It.IsAny<IInjector>(), It.Is<IInterface_1>(inst => inst is Implementation_1)))
                .Returns<IInjector, IInterface_1>((injector, inst) => inst);

            var mockCallback2 = new Mock<Func<IInjector, IInterface_1, IInterface_1>>(MockBehavior.Strict);
            mockCallback2
                .Setup(_ => _(It.IsAny<IInjector>(), It.Is<IInterface_1>(inst => inst is Implementation_1)))
                .Returns(new DecoratedImplementation_1());

            Container
                .Service<IInterface_1, Implementation_1>(lifetime)
                .Proxy(mockCallback1.Object)
                .Proxy(mockCallback2.Object);

            Assert.That(Container.Get<IInterface_1>().GetService(null), Is.InstanceOf<DecoratedImplementation_1>());
            mockCallback1.Verify(_ => _(It.IsAny<IInjector>(), It.IsAny<IInterface_1>()), Times.Once);
            mockCallback2.Verify(_ => _(It.IsAny<IInjector>(), It.IsAny<IInterface_1>()), Times.Once);
        }

        [Test]
        public void Container_Proxy_ShouldWorkWithGenericServices()
        {
            var mockCallback = new Mock<Func<IInjector, IInterface_3<int>, IInterface_3<int>>>(MockBehavior.Strict);
            mockCallback
                .Setup(_ => _(It.IsAny<IInjector>(), It.Is<IInterface_3<int>>(inst => inst is Implementation_3<int>)))
                .Returns(new DecoratedImplementation_3<int>());

            Container
                .Service(typeof(IInterface_3<>), typeof(Implementation_3<>))
                .Proxy(mockCallback.Object);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_1)), null, It.Is<Type>(t => t == typeof(Implementation_3<int>))))
                .Returns(new Implementation_1());

            //
            // Nem kell QueryMode.AllowSpecialization mert a Proxy() hivas mar
            // rogzitette az uj elemet.
            //

            Assert.That(Container.Get<IInterface_3<int>>(QueryMode.ThrowOnError).GetService(mockInjector.Object), Is.InstanceOf<DecoratedImplementation_3<int>>());
            mockCallback.Verify(_ => _(It.IsAny<IInjector>(), It.IsAny<IInterface_3<int>>()), Times.Once);
        }

        [Test]
        public void Container_Proxy_ShouldWorkWithLazyServices()
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(typeof(Implementation_1));
            mockResolver
                .Setup(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(true);

            Container
                .Lazy<IInterface_1>(mockResolver.Object)
                .Proxy<IInterface_1>((injector, inst) => new DecoratedImplementation_1());

            //
            // Az elso Get()-eleskor kell hivja a rendszer a resolver-t
            //

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Never);

            Assert.That(Container.Get<IInterface_1>().GetService(null), Is.InstanceOf<DecoratedImplementation_1>());     
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnOpenGenericParameter()
        {
            Container.Service(typeof(IInterface_3<>), typeof(Implementation_3<>));

            Assert.Throws<InvalidOperationException>(() => Container.Proxy(typeof(IInterface_3<>), (injector, type, inst) => inst), Resources.CANT_PROXY);
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnNotOwnedSingletonService()
        {
            Container.Service<IInterface_1, Implementation_1>(Lifetime.Singleton);
            Assert.DoesNotThrow(() => Container.Proxy<IInterface_1>((i, c) => c));

            using (IServiceContainer child = Container.CreateChild())
            {
                Assert.Throws<InvalidOperationException>(() => child.Proxy<IInterface_1>((i, c) => c), Resources.CANT_PROXY);
            }
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnInstances()
        {
            Container.Instance<IInterface_1>(new Implementation_1());

            Assert.Throws<InvalidOperationException>(() => Container.Proxy<IInterface_1>((p1, p2) => default), Resources.CANT_PROXY);
        }

        [Test]
        public void Container_Proxy_ShouldThrowOnAbstractService()
        {
            Container.Abstract<IInterface_1>();

            Assert.Throws<InvalidOperationException>(() => Container.Proxy<IInterface_1>((p1, p2) => default), Resources.CANT_PROXY);
        }

        [Test]
        public void Container_Proxy_MayHaveDependency()
        {
            Container
                .Service<IInterface_2, Implementation_2>()
                .Proxy<IInterface_2, MyProxyWithDependency>();

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_1)), null, It.Is<Type>(t => typeof(MyProxyWithDependency).IsAssignableFrom(t))))
                .Returns(new Implementation_1());

            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_1)), null, It.Is<Type>(t => t == typeof(Implementation_2))))
                .Returns(new Implementation_1());

            object instance =  Container.Get<IInterface_2>().GetService(mockInjector.Object);

            Assert.That(instance, Is.InstanceOf<MyProxyWithDependency>());

            MyProxyWithDependency implementor = (MyProxyWithDependency) instance;

            Assert.That(implementor.Dependency, Is.InstanceOf<Implementation_1>());
            Assert.That(implementor.Target, Is.InstanceOf<Implementation_2>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_1)), null, It.Is<Type>(t => typeof(MyProxyWithDependency).IsAssignableFrom(t))), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_1)), null, It.Is<Type>(t => t == typeof(Implementation_2))), Times.Once);
        }

        public class MyProxyWithDependency : InterfaceInterceptor<IInterface_2>
        {
            public MyProxyWithDependency(IInterface_1 dependency, IInterface_2 target) : base(target)
            {
                Dependency = dependency;
            }

            public IInterface_1 Dependency { get; }
        }

        [Test]
        public void Container_Bulked_ProxyingTest() 
        {
            Container
                .Setup(typeof(ContainerTestsBase<>).Assembly())
                .Service<IDisposable, Disposable>();

            foreach (AbstractServiceEntry entry in Container.Where(e => typeof(IModule).IsAssignableFrom(e.Interface)))
                Container.Proxy(entry.Interface, typeof(InterfaceInterceptor<>).MakeGenericType(entry.Interface));

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            Assert.That(Container.Get<IDisposable>().GetService(mockInjector.Object) is Disposable);
            Assert.That(Container.Get<IMyModule1>().GetService(mockInjector.Object)  is InterfaceInterceptor<IMyModule1>);
            Assert.That(Container.Get<IMyModule2>().GetService(mockInjector.Object)  is InterfaceInterceptor<IMyModule2>);
        }

        [Test]
        public void Container_Proxy_ShouldHandleNamedServices() 
        {
            Container.Service<IInterface_1, Implementation_1>("cica");
            Assert.DoesNotThrow(() => Container.Proxy<IInterface_1>("cica", (injector, svc) => new DecoratedImplementation_1()));
            Assert.That(Container.Get<IInterface_1>("cica").GetService(null), Is.TypeOf<DecoratedImplementation_1>());
        }
    }

    public interface IModule { }
    public interface IMyModule1 : IModule { }
    public interface IMyModule2 : IModule { }
    [Service(typeof(IMyModule1))]
    public class Module1 : IMyModule1 { }
    [Service(typeof(IMyModule2))]
    public class Module2 : IMyModule2 { }
}
