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

namespace Solti.Utils.DI.Tests
{
    using Internals;
    using Interfaces;
    using Interfaces.Properties;
    using Primitives.Patterns;
    using Primitives.Threading;
    using Proxy;

    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Aspects_ProxyInstallationShouldBeDoneOnBuild([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockInjector = new Mock<IInstanceFactory>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(new Mock<ILifetimeManager<object>>(MockBehavior.Strict).Object);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service<IMyService, MyService>(lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor());

            IMyService instance = (IMyService) lastEntry
                .CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<InterfaceInterceptor<IMyService>>());

            instance = ((InterfaceInterceptor<IMyService>) instance).Target;
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<MyService>());
        }

        [Test]
        public void Aspects_ProxyMayHaveDependency([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockInjector = new Mock<IInstanceFactory>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null))
                .Returns(new Disposable());
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(new Mock<ILifetimeManager<object>>(MockBehavior.Strict).Object);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service<IMyDependantService, MyService>(lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor());

            lastEntry.CreateInstance(mockInjector.Object, out object _);

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
        }

        [Test]
        public void Aspects_ShouldWorkWithGenericServices([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockInjector = new Mock<IInstanceFactory>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(new Mock<ILifetimeManager<object>>(MockBehavior.Strict).Object);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service(typeof(IMyGenericService<>), typeof(MyGenericService<>), lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            Assert.That(lastEntry.Factory, Is.Null);

            lastEntry = lastEntry.Specialize(typeof(int));
            Assert.DoesNotThrow(() => lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor()));

            IMyGenericService<int> instance = (IMyGenericService<int>) lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<InterfaceInterceptor<IMyGenericService<int>>>());

            instance = ((InterfaceInterceptor<IMyGenericService<int>>) instance).Target;
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<MyGenericService<int>>());
        }

        [Test]
        public void Aspects_ShouldThrowOnInstances() =>
            Assert.Throws<NotSupportedException>(() => Collection.Instance<IMyService>(new MyService()).ApplyProxy((_, _, _) => null), Resources.PROXYING_NOT_SUPPORTED);

        [Test]
        public void Aspects_ApplyingAspectsShouldBeSequential([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockService = new Mock<IOrderInspectingService>(MockBehavior.Strict);
            mockService
                .Setup(x => x.GetAspectsOrder())
                .Returns(Array.Empty<string>());

            var mockInjector = new Mock<IInstanceFactory>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(new Mock<ILifetimeManager<object>>(MockBehavior.Strict).Object);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Factory(i => mockService.Object, lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor());

            IOrderInspectingService svc = (IOrderInspectingService) lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(svc.GetAspectsOrder().SequenceEqual(new[]
            {
                nameof(OrderInspectingAspect1Attribute),
                nameof(OrderInspectingAspect2Attribute),
                nameof(OrderInspectingAspect3Attribute)
            }));
        }

        public class InterceptorHavingNoTarget : InterfaceInterceptor<IInterface_1>
        {
            public InterceptorHavingNoTarget() : base(null)
            {
            }
        }

        public class InterceptorHavingMultipleTarget : InterfaceInterceptor<IInterface_1>
        {
            public InterceptorHavingMultipleTarget(IInterface_1 target, IInterface_1 target2) : base(target)
            {
            }
        }

        [Test]
        public void Aspects_ApplyingAspectsShouldThrowIfThereIsNoTargetCtorParameter([Values(typeof(InterceptorHavingNoTarget), typeof(InterceptorHavingMultipleTarget))] Type interceptor) =>
            Assert.Throws<InvalidOperationException>(() => new ScopedServiceEntry(typeof(IInterface_1), null, typeof(Implementation_1_No_Dep)).ApplyProxy(interceptor), Properties.Resources.TARGET_PARAM_CANNOT_BE_DETERMINED);
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
    public class DummyAspectAttribute : AspectAttribute, IInterceptorFactory<Type>
    {
        public Type GetInterceptor(Type iface) => typeof(InterfaceInterceptor<>).MakeGenericType(iface);
    }

    public class MyInterceptorHavingDependency<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
    {
        public MyInterceptorHavingDependency(IDisposable dep, TInterface target) : base(target) { }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class DummyAspectHavingDependencyAttribute : AspectAttribute, IInterceptorFactory<Type>
    {
        public Type GetInterceptor(Type iface) => typeof(MyInterceptorHavingDependency<>).MakeGenericType(iface);
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
    public class OrderInspectingAspect1Attribute : AspectAttribute, IInterceptorFactory<Type>
    {
        public class OrderInspectingProxy<TInterface> : OrderInspectingProxyBase<TInterface> where TInterface : class
        {
            public OrderInspectingProxy(TInterface target) : base(target, nameof(OrderInspectingAspect1Attribute)) { }
        }

        public Type GetInterceptor(Type iface) => typeof(OrderInspectingProxy<>).MakeGenericType(iface);
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect2Attribute : AspectAttribute, IInterceptorFactory<Type>
    {
        public class OrderInspectingProxy<TInterface> : OrderInspectingProxyBase<TInterface> where TInterface : class
        {
            public OrderInspectingProxy(TInterface target) : base(target, nameof(OrderInspectingAspect2Attribute)) { }
        }

        public Type GetInterceptor(Type iface) => typeof(OrderInspectingProxy<>).MakeGenericType(iface);
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect3Attribute : AspectAttribute, IInterceptorFactory<Type>
    {
        public class OrderInspectingProxy<TInterface> : OrderInspectingProxyBase<TInterface> where TInterface : class
        {
            public OrderInspectingProxy(TInterface target) : base(target, nameof(OrderInspectingAspect3Attribute)) { }
        }

        public Type GetInterceptor(Type iface) => typeof(OrderInspectingProxy<>).MakeGenericType(iface);
    }

    [OrderInspectingAspect1, OrderInspectingAspect2, OrderInspectingAspect3]
    public interface IOrderInspectingService
    {
        IEnumerable<string> GetAspectsOrder();
    }
}
