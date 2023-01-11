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

    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Aspects_MayBeDisabled([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Collection = new ServiceCollection
            (
                new ServiceOptions
                {
                    SupportAspects = false
                }
            );

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

            Collection.Service<IMyServiceHavingAspect, MyService>(lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor());

            object instance = lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<MyService>());
        }

        [Test]
        public void Aspects_MayBeDisabled_GenericCase([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Collection = new ServiceCollection
            (
                new ServiceOptions
                {
                    SupportAspects = false
                }
            );

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

            Collection.Service(typeof(IMyGenericServiceHavingAspect<>), typeof(MyGenericService<>), lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry.Specialize(typeof(int));
            lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor());

            object instance = lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<MyGenericService<int>>());
        }

        [Test]
        public void Aspects_ProxyInstallationShouldBeDoneOnBuild_InterfaceAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
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

            Collection.Service<IMyServiceHavingAspect, MyService>(lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor());

            IMyServiceHavingAspect instance = (IMyServiceHavingAspect) lastEntry
                .CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<AspectAggregator<IMyServiceHavingAspect, MyService>>());

            instance = ((AspectAggregator<IMyServiceHavingAspect, MyService>) instance).Target;
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void Aspects_ProxyInstallationShouldBeDoneOnBuild_ClassAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
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

            Collection.Service<IMyService, MyServiceHavingAspect>(lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor());

            IMyService instance = (IMyService) lastEntry
                .CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<AspectAggregator<IMyService, MyServiceHavingAspect>>());

            instance = ((AspectAggregator<IMyService, MyServiceHavingAspect>)instance).Target;
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void Aspects_MayHaveDependency_InterfaceAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
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

            Collection.Factory(i => new Mock<IMyServiceHavingDependantAspect>().Object, lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor());

            lastEntry.CreateInstance(mockInjector.Object, out object _);

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
        }

        [Test]
        public void Aspects_MayHaveDependency_ClassAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
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

            Collection.Service<IMyService, MyServiceHavingDependantAspect>(lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor());

            lastEntry.CreateInstance(mockInjector.Object, out object _);

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
        }

        [Test]
        public void Aspects_ShouldWorkWithGenericServices_InterfaceAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
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

            Collection.Service(typeof(IMyGenericServiceHavingAspect<>), typeof(MyGenericService<>), lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            Assert.That(lastEntry.Factory, Is.Null);

            lastEntry = lastEntry.Specialize(typeof(int));
            Assert.DoesNotThrow(() => lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor()));

            IMyGenericServiceHavingAspect<int> instance = (IMyGenericServiceHavingAspect<int>) lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<AspectAggregator<IMyGenericServiceHavingAspect<int>, MyGenericService<int>>>());

            instance = ((AspectAggregator<IMyGenericServiceHavingAspect<int>, MyGenericService<int>>) instance).Target;
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void Aspects_ShouldWorkWithGenericServices_ClassAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
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

            Collection.Service(typeof(IMyGenericService<>), typeof(MyGenericServiceHavingAspect<>), lifetime);

            AbstractServiceEntry lastEntry = Collection.LastEntry;
            Assert.That(lastEntry.Factory, Is.Null);

            lastEntry = lastEntry.Specialize(typeof(int));
            Assert.DoesNotThrow(() => lastEntry.Build(mockBuildContext.Object, new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor()));

            IMyGenericService<int> instance = (IMyGenericService<int>) lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<AspectAggregator<IMyGenericService<int>, MyGenericServiceHavingAspect<int>>>());

            instance = ((AspectAggregator<IMyGenericService<int>, MyGenericServiceHavingAspect<int>>) instance).Target;
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void Aspects_ShouldThrowOnInstances() =>
            Assert.Throws<NotSupportedException>(() => Collection.Instance<IMyServiceHavingAspect>(new MyService()).Decorate((_, _, _) => null), Resources.DECORATING_NOT_SUPPORTED);

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
    }

    public interface IMyService { }

    public interface IMyGenericService<T> { }

    [DummyAspect]
    public interface IMyServiceHavingAspect
    {
    }

    [DummyAspect]
    public interface IMyGenericServiceHavingAspect<T> { }

    [DummyAspectHavingDependency]
    public interface IMyServiceHavingDependantAspect { }

    public class MyService : IMyServiceHavingAspect { }

    public class MyGenericService<T> : IMyGenericServiceHavingAspect<T> { }

    [DummyAspect]
    public class MyServiceHavingAspect : IMyService { }

    [DummyAspectHavingDependency]
    public class MyServiceHavingDependantAspect : IMyService { }

    [DummyAspect]
    public class MyGenericServiceHavingAspect<T> : IMyGenericService<T> { }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class DummyAspectAttribute : AspectAttribute
    {
        private sealed class DummyInterceptor : IInterfaceInterceptor
        {
            public object Invoke(IInvocationContext context, InvokeInterceptorDelegate callNext) => callNext();
        }

        public override Type UnderlyingInterceptor { get; } = typeof(DummyInterceptor);
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class DummyAspectHavingDependencyAttribute : AspectAttribute
    {
        private sealed class DummyInterceptorHavingDependency : IInterfaceInterceptor
        {
            public DummyInterceptorHavingDependency(IDisposable dep) { }

            public object Invoke(IInvocationContext context, InvokeInterceptorDelegate callNext) => callNext();
        }

        public override Type UnderlyingInterceptor { get; } = typeof(DummyInterceptorHavingDependency);
    }

    public abstract class OrderInspectingInterceptorBase : IInterfaceInterceptor
    {
        public string Name { get; }

        public OrderInspectingInterceptorBase(string name) => Name = name;

        public object Invoke(IInvocationContext context, InvokeInterceptorDelegate callNext)
        {
            IEnumerable<string> result = (IEnumerable<string>) callNext();

            return result.Append(Name);
        }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect1Attribute : AspectAttribute
    {
        private sealed class OrderInspectingInterceptor : OrderInspectingInterceptorBase
        {
            public OrderInspectingInterceptor() : base(nameof(OrderInspectingAspect1Attribute)) { }
        }

        public override Type UnderlyingInterceptor { get; } = typeof(OrderInspectingInterceptor);
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect2Attribute : AspectAttribute
    {
        private sealed class OrderInspectingInterceptor : OrderInspectingInterceptorBase
        {
            public OrderInspectingInterceptor() : base(nameof(OrderInspectingAspect2Attribute)) { }
        }

        public override Type UnderlyingInterceptor { get; } = typeof(OrderInspectingInterceptor);
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect3Attribute : AspectAttribute
    {
        private sealed class OrderInspectingInterceptor : OrderInspectingInterceptorBase
        {
            public OrderInspectingInterceptor() : base(nameof(OrderInspectingAspect3Attribute)) { }
        }

        public override Type UnderlyingInterceptor { get; } = typeof(OrderInspectingInterceptor);
    }

    [OrderInspectingAspect1, OrderInspectingAspect2, OrderInspectingAspect3]
    public interface IOrderInspectingService
    {
        IEnumerable<string> GetAspectsOrder();
    }
}
