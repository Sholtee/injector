/********************************************************************************
* Aspect.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Internals;
    using Properties;
    using Proxy;

    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Aspects_ShouldNotBeAppliedAgainstAbstractServices() 
        {
            Container.Abstract<IMyService>();

            Assert.That(Container.Get<IMyService>().Factory, Is.Null);
        }

        [Test]
        public void Aspects_ProxyInstallationShouldBeDoneOnServiceRegistration([Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime) 
        {
            Container.Service<IMyService, MyService>(lifetime);

            IMyService instance = (IMyService) Container
                .Get<IMyService>()
                .Factory
                .Invoke(new Mock<IInjector>(MockBehavior.Strict).Object, typeof(IMyService));

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<InterfaceInterceptor<IMyService>>());

            instance = ((InterfaceInterceptor<IMyService>) instance).Target;
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<MyService>());
        }

        [Test]
        public void Aspects_ProxyMayHaveDependency() 
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null))
                .Returns(new Disposable());

            Container.Service<IMyDependantService, MyService>();

            Container
                .Get<IMyDependantService>()
                .Factory
                .Invoke(mockInjector.Object, typeof(IMyDependantService));

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
        }

        [Test]
        public void Aspects_ServiceInheritanceShouldNotTriggerTheProxyRegistration([Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime) 
        {
            Container.Service<IMyService, MyService>(lifetime);

            IServiceContainer child = Container.CreateChild();

            Assert.AreSame(child.Get<IMyService>().Factory, Container.Get<IMyService>().Factory);
        }

        [Test]
        public void Aspects_ShouldWorkWithGenericServices() 
        {
            Container.Service(typeof(IMyGenericService<>), typeof(MyGenericService<>));

            Assert.That(Container.Get(typeof(IMyGenericService<>)).Factory, Is.Null);

            IMyGenericService<int> instance = (IMyGenericService<int>) Container
                .Get<IMyGenericService<int>>(QueryModes.AllowSpecialization)
                .Factory
                .Invoke(new Mock<IInjector>(MockBehavior.Strict).Object, typeof(IMyService));

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<InterfaceInterceptor<IMyGenericService<int>>>());

            instance = ((InterfaceInterceptor<IMyGenericService<int>>) instance).Target;
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<MyGenericService<int>>());
        }

        [Test]
        public void Aspects_ShouldThrowOnInstances() =>
            Assert.Throws<InvalidOperationException>(() => Container.Instance<IMyService>(new MyService()), Resources.CANT_PROXY);
    }

    [DummyAspect]
    public interface IMyService
    {
    }

    [DummyAspect]
    public interface IMyGenericService<T> { }

    [DummyAspectHavingDependency]
    public interface IMyDependantService { }

    public class MyService : IMyService, IMyDependantService { }

    public class MyGenericService<T> : IMyGenericService<T> { }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class DummyAspectAttribute : AspectAttribute
    {
        public override Type GetInterceptor(Type iface) => typeof(InterfaceInterceptor<>).MakeGenericType(iface);
    }

    public class MyInterceptorHavingDependency<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
    {
        public MyInterceptorHavingDependency(IDisposable dep, TInterface target) : base(target) { }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class DummyAspectHavingDependencyAttribute : AspectAttribute
    {
        public override Type GetInterceptor(Type iface) => typeof(MyInterceptorHavingDependency<>).MakeGenericType(iface);
    }
}
