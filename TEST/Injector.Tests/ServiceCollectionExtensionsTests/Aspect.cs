/********************************************************************************
* Aspect.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.ServiceCollection.Tests
{
    using Interfaces;
    using Interfaces.Properties;
    using Primitives.Patterns;
    using Proxy;

    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Aspects_ProxyInstallationShouldBeDoneOnServiceRegistration([ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            Collection.Service<IMyService, MyService>(lifetime);

            IMyService instance = (IMyService) Collection
                .LastEntry
                .Factory
                .Invoke(new Mock<IInjector>(MockBehavior.Strict).Object, typeof(IMyService));

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<InterfaceInterceptor<IMyService>>());

            instance = ((InterfaceInterceptor<IMyService>) instance).Target;
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<MyService>());
        }

        [Test]
        public void Aspects_ProxyMayHaveDependency([ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null))
                .Returns(new Disposable());

            Collection.Service<IMyDependantService, MyService>(lifetime);

            Collection
                .LastEntry
                .Factory
                .Invoke(mockInjector.Object, typeof(IMyDependantService));

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
        }

        [Test]
        public void Aspects_ShouldWorkWithGenericServices([ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            Collection.Service(typeof(IMyGenericService<>), typeof(MyGenericService<>), lifetime);

            Assert.That(Collection.LastEntry.Factory, Is.Null);

            IMyGenericService<int> instance = (IMyGenericService<int>) ((ISupportsSpecialization) Collection.LastEntry)
                .Specialize(new Mock<IServiceRegistry>(MockBehavior.Strict).Object, typeof(int))
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
            Assert.Throws<InvalidOperationException>(() => Collection.Instance<IMyService>(new MyService()).WithProxy((_, _, _) => null), Resources.PROXYING_NOT_SUPPORTED);

        [Test]
        public void Aspects_AspectAttributeMustBeOverridden() 
        {
            var attr = new AspectWithoutImplementation();
            Assert.Throws<NotImplementedException>(() => attr.GetInterceptorType(null));
            Assert.Throws<NotImplementedException>(() => attr.GetInterceptor(null, null, null));
        }

        [Test]
        public void Aspects_ShouldBeControlledByAspectKind([Values(AspectKind.Service, AspectKind.Factory)] AspectKind kind, [ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            Collection.Factory<IMyService>(i => new MyService(), lifetime);

            AbstractServiceEntry entry = Collection.LastEntry;

            var mockAspect = new Mock<AspectWithoutImplementation>(MockBehavior.Strict, kind);

            Func<IInjector, Type, object, object>[] delegates = null;

            switch (kind)
            {
                case AspectKind.Service:
                    mockAspect
                        .Setup(aspect => aspect.GetInterceptorType(It.Is<Type>(t => t == typeof(IMyService))))
                        .Returns(typeof(InterfaceInterceptor<IMyService>));
                    
                    Assert.DoesNotThrow(() => delegates = Internals.ServiceEntryExtensions.GenerateProxyDelegates(typeof(IMyService), new[] { mockAspect.Object }));

                    mockAspect.Verify(aspect => aspect.GetInterceptorType(It.Is<Type>(t => t == typeof(IMyService))), Times.Once);
                    Assert.That(delegates.Length, Is.EqualTo(1));

                    break;
                case AspectKind.Factory:
                    var decorated = new MyService();
                    mockAspect
                        .Setup(aspect => aspect.GetInterceptor(It.IsAny<IInjector>(), It.Is<Type>(t => t == typeof(IMyService)), It.IsAny<IMyService>()))
                        .Returns(decorated);

                    Assert.DoesNotThrow(() => delegates = Internals.ServiceEntryExtensions.GenerateProxyDelegates(typeof(IMyService), new[] { mockAspect.Object }));
                    Assert.That(delegates.Single(), Is.EqualTo((Func<IInjector, Type, object, object>) mockAspect.Object.GetInterceptor));

                    break;
            }
        }

        [Test]
        public void Aspects_ApplyingAspectsShouldBeSequential([ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            var mockService = new Mock<IOrderInspectingService>(MockBehavior.Strict);
            mockService
                .Setup(x => x.GetAspectsOrder())
                .Returns(Array.Empty<string>());

            Collection.Factory(i => mockService.Object, lifetime);

            var svc = (IOrderInspectingService) Collection
                .LastEntry
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
        public override Type GetInterceptorType(Type iface) => typeof(InterfaceInterceptor<>).MakeGenericType(iface);
    }

    public class MyInterceptorHavingDependency<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
    {
        public MyInterceptorHavingDependency(IDisposable dep, TInterface target) : base(target) { }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class DummyAspectHavingDependencyAttribute : AspectAttribute
    {
        public override Type GetInterceptorType(Type iface) => typeof(MyInterceptorHavingDependency<>).MakeGenericType(iface);
    }

    public abstract class OrderInspectingProxyBase<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
    {
        public string Name { get; }

        public OrderInspectingProxyBase(TInterface target, string name) : base(target) => Name = name;

        public override object Invoke(InvocationContext context)
        {
            IEnumerable<string> result = (IEnumerable<string>) base.Invoke(context);

            return result.Append(Name);
        }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect1Attribute : AspectAttribute
    {
        public class OrderInspectingProxy<TInterface> : OrderInspectingProxyBase<TInterface> where TInterface : class
        {
            public OrderInspectingProxy(TInterface target) : base(target, nameof(OrderInspectingAspect1Attribute)) { }
        }

        public override Type GetInterceptorType(Type iface) => typeof(OrderInspectingProxy<>).MakeGenericType(iface);
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect2Attribute : AspectAttribute
    {
        public class OrderInspectingProxy<TInterface> : OrderInspectingProxyBase<TInterface> where TInterface : class
        {
            public OrderInspectingProxy(TInterface target) : base(target, nameof(OrderInspectingAspect2Attribute)) { }
        }

        public override Type GetInterceptorType(Type iface) => typeof(OrderInspectingProxy<>).MakeGenericType(iface);
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect3Attribute : AspectAttribute
    {
        public class OrderInspectingProxy<TInterface> : OrderInspectingProxyBase<TInterface> where TInterface : class
        {
            public OrderInspectingProxy(TInterface target) : base(target, nameof(OrderInspectingAspect3Attribute)) { }
        }

        public override Type GetInterceptorType(Type iface) => typeof(OrderInspectingProxy<>).MakeGenericType(iface);
    }

    [OrderInspectingAspect1, OrderInspectingAspect2, OrderInspectingAspect3]
    public interface IOrderInspectingService
    {
        IEnumerable<string> GetAspectsOrder();
    }
}
