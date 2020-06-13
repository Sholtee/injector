/********************************************************************************
* Aspect.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Interfaces;
    using Internals;
    using Primitives;
    using Primitives.Patterns;
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

        [Test]
        public void Aspects_AspectAttributeMustBeOverridden() 
        {
            var attr = new AspectWithoutImplementation();
            Assert.Throws<NotImplementedException>(() => attr.GetInterceptor(null));
            Assert.Throws<NotImplementedException>(() => attr.GetInterceptor(null, null, null));
        }

        [Test]
        public void Aspects_ApplyAspectShouldBeControlledByAttributeKind([Values(AspectKind.Service, AspectKind.Factory)] AspectKind kind) 
        {
            Container.Factory<IMyService>(i => new MyService());

            AbstractServiceEntry entry = Container.Get<IMyService>();

            var mockAspect = new Mock<AspectWithoutImplementation>(MockBehavior.Strict, kind);

            switch (kind)
            {
                case AspectKind.Service:
                    mockAspect
                        .Setup(aspect => aspect.GetInterceptor(It.Is<Type>(t => t == typeof(IMyService))))
                        .Returns(typeof(InterfaceInterceptor<IMyService>));
                    entry.ApplyAspect(mockAspect.Object);

                    mockAspect.Verify(aspect => aspect.GetInterceptor(It.Is<Type>(t => t == typeof(IMyService))), Times.Once);
                    Assert.That(entry.Factory.Invoke(new Mock<IInjector>(MockBehavior.Strict).Object, entry.Interface), Is.InstanceOf<InterfaceInterceptor<IMyService>>());

                    break;
                case AspectKind.Factory:
                    var decorated = new MyService();
                    mockAspect
                        .Setup(aspect => aspect.GetInterceptor(It.IsAny<IInjector>(), It.Is<Type>(t => t == typeof(IMyService)), It.IsAny<IMyService>()))
                        .Returns(decorated);
                    entry.ApplyAspect(mockAspect.Object);
                  
                    Assert.That(entry.Factory.Invoke(new Mock<IInjector>(MockBehavior.Strict).Object, entry.Interface), Is.SameAs(decorated));
                    mockAspect.Verify(aspect => aspect.GetInterceptor(It.IsAny<IInjector>(), It.Is<Type>(t => t == typeof(IMyService)), It.IsAny<IMyService>()), Times.Once);

                    break;
            }
        }

        [Test]
        public void Aspects_ApplyingAspectsShouldBeSequential() 
        {
            var mockService = new Mock<IOrderInspectingService>(MockBehavior.Strict);
            mockService
                .Setup(x => x.GetAspectsOrder())
                .Returns(Array.Empty<string>());

            Container.Factory(i => mockService.Object);

            var svc = (IOrderInspectingService) Container
                .Get<IOrderInspectingService>()
                .Factory(new Mock<IInjector>(MockBehavior.Strict).Object, typeof(IOrderInspectingService));

            Assert.That(svc.GetAspectsOrder().SequenceEqual(new[] { nameof(OrderInspectingAspect1Attribute), nameof(OrderInspectingAspect2Attribute), nameof(OrderInspectingAspect3Attribute) }));
        }
    }

    public class AspectWithoutImplementation : AspectAttribute
    {
        public AspectWithoutImplementation(AspectKind kind = AspectKind.Service) => Kind = kind;
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

    public abstract class OrderInspectingProxyBase<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
    {
        public string Name { get; }

        public OrderInspectingProxyBase(TInterface target, string name) : base(target) => Name = name;

        public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
        {
            IEnumerable<string> result = (IEnumerable<string>) method.ToInstanceDelegate().Invoke(Target, Array.Empty<object>());

            return result.Append(Name);
        }
    }

    public class OrderInspectingAspect1Attribute : AspectAttribute
    {
        public class OrderInspectingProxy<TInterface> : OrderInspectingProxyBase<TInterface> where TInterface : class
        {
            public OrderInspectingProxy(TInterface target) : base(target, nameof(OrderInspectingAspect1Attribute)) { }
        }

        public override Type GetInterceptor(Type iface) => typeof(OrderInspectingProxy<>).MakeGenericType(iface);
    }

    public class OrderInspectingAspect2Attribute : AspectAttribute
    {
        public class OrderInspectingProxy<TInterface> : OrderInspectingProxyBase<TInterface> where TInterface : class
        {
            public OrderInspectingProxy(TInterface target) : base(target, nameof(OrderInspectingAspect2Attribute)) { }
        }

        public override Type GetInterceptor(Type iface) => typeof(OrderInspectingProxy<>).MakeGenericType(iface);
    }

    public class OrderInspectingAspect3Attribute : AspectAttribute
    {
        public class OrderInspectingProxy<TInterface> : OrderInspectingProxyBase<TInterface> where TInterface : class
        {
            public OrderInspectingProxy(TInterface target) : base(target, nameof(OrderInspectingAspect3Attribute)) { }
        }

        public override Type GetInterceptor(Type iface) => typeof(OrderInspectingProxy<>).MakeGenericType(iface);
    }


    [OrderInspectingAspect1]
    [OrderInspectingAspect2]
    [OrderInspectingAspect3]
    public interface IOrderInspectingService
    {
        IEnumerable<string> GetAspectsOrder();
    }
}
