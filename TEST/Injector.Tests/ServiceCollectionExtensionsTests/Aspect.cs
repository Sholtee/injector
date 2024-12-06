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
    using Primitives;
    using Primitives.Patterns;

    public partial class ServiceCollectionExtensionsTests
    {
        protected DelegateCompiler Compiler { get; set; }

        [SetUp]
        public void Setup() => Compiler = new DelegateCompiler();

        [Test]
        public void Aspects_MayBeDisabled([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Collection = new ServiceCollection();

            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(PooledLifetime.POOL_SCOPE);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(Compiler);
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service<IMyServiceHavingAspect, MyService>(lifetime, ServiceOptions.Default with { SupportAspects = false});

            AbstractServiceEntry lastEntry = Collection.Last();
            lastEntry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            Compiler.Compile();

            object instance = lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<MyService>());
        }

        [Test]
        public void Aspects_MayBeDisabled_GenericCase([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Collection = new ServiceCollection();

            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(PooledLifetime.POOL_SCOPE);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(Compiler);
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service(typeof(IMyGenericServiceHavingAspect<>), typeof(MyGenericService<>), lifetime, ServiceOptions.Default with { SupportAspects = false });

            AbstractServiceEntry lastEntry = Collection.Last().Specialize(typeof(int));
            lastEntry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            Compiler.Compile();

            object instance = lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<MyGenericService<int>>());
        }

        [Test]
        public void Aspects_ProxyInstallationShouldBeDoneOnBuild_InterfaceAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(PooledLifetime.POOL_SCOPE);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(Compiler);
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service<IMyServiceHavingAspect, MyService>(lifetime);

            AbstractServiceEntry lastEntry = Collection.Last();
            lastEntry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            Compiler.Compile();

            IMyServiceHavingAspect instance = (IMyServiceHavingAspect) lastEntry
                .CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<InterceptorAggregator<IMyServiceHavingAspect, IMyServiceHavingAspect>>());

            instance = ((InterceptorAggregator<IMyServiceHavingAspect, IMyServiceHavingAspect>) instance).Target;
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void Aspects_ProxyInstallationShouldBeDoneOnBuild_ClassAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(PooledLifetime.POOL_SCOPE);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(Compiler);
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service<IMyService, MyServiceHavingAspect>(lifetime);

            AbstractServiceEntry lastEntry = Collection.Last();
            lastEntry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            Compiler.Compile();

            IMyService instance = (IMyService) lastEntry
                .CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<InterceptorAggregator<IMyService, MyServiceHavingAspect>>());

            instance = ((InterceptorAggregator<IMyService, MyServiceHavingAspect>)instance).Target;
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void Aspects_MayHaveDependency_InterfaceAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null))
                .Returns(new Disposable());
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(PooledLifetime.POOL_SCOPE);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(Compiler);
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Factory(i => new Mock<IMyServiceHavingDependantAspect>().Object, lifetime);

            AbstractServiceEntry lastEntry = Collection.Last();
            lastEntry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            Compiler.Compile();

            lastEntry.CreateInstance(mockInjector.Object, out object _);

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
        }

        [Test]
        public void Aspects_MayHaveDependency_ClassAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null))
                .Returns(new Disposable());
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(PooledLifetime.POOL_SCOPE);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(Compiler);
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service<IMyService, MyServiceHavingDependantAspect>(lifetime);

            AbstractServiceEntry lastEntry = Collection.Last();
            lastEntry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            Compiler.Compile();

            lastEntry.CreateInstance(mockInjector.Object, out object _);

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
        }

        [Test]
        public void Aspects_ShouldWorkWithGenericServices_InterfaceAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(PooledLifetime.POOL_SCOPE);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(Compiler);
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service(typeof(IMyGenericServiceHavingAspect<>), typeof(MyGenericService<>), lifetime);

            AbstractServiceEntry lastEntry = Collection.Last();
            Assert.That(lastEntry.Factory, Is.Null);

            lastEntry = lastEntry.Specialize(typeof(int));
            Assert.DoesNotThrow(() => lastEntry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() }));
            Compiler.Compile();

            IMyGenericServiceHavingAspect<int> instance = (IMyGenericServiceHavingAspect<int>) lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<InterceptorAggregator<IMyGenericServiceHavingAspect<int>, IMyGenericServiceHavingAspect<int>>>());

            instance = ((InterceptorAggregator<IMyGenericServiceHavingAspect<int>, IMyGenericServiceHavingAspect<int>>) instance).Target;
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void Aspects_ShouldWorkWithGenericServices_ClassAspect([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(PooledLifetime.POOL_SCOPE);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(Compiler);
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Service(typeof(IMyGenericService<>), typeof(MyGenericServiceHavingAspect<>), lifetime);

            AbstractServiceEntry lastEntry = Collection.Last();
            Assert.That(lastEntry.Factory, Is.Null);

            lastEntry = lastEntry.Specialize(typeof(int));
            Assert.DoesNotThrow(() => lastEntry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() }));
            Compiler.Compile();

            IMyGenericService<int> instance = (IMyGenericService<int>) lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<InterceptorAggregator<IMyGenericService<int>, MyGenericServiceHavingAspect<int>>>());

            instance = ((InterceptorAggregator<IMyGenericService<int>, MyGenericServiceHavingAspect<int>>) instance).Target;
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void Aspects_ShouldThrowOnInstances() =>
            Assert.Throws<NotSupportedException>(() => Collection.Instance<IMyServiceHavingAspect>(new MyService()), Resources.DECORATING_NOT_SUPPORTED);


        [Test]
        public void Aspects_ShouldThrowOnNonInterfaceServiceType() =>
            Assert.Throws<NotSupportedException>(() => Collection.Service<MyServiceHavingAspect, MyServiceHavingAspect>(Lifetime.Scoped));

        [Test]
        public void Aspects_ShoulBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => new AspectAttribute(interceptor: null));
            Assert.Throws<ArgumentNullException>(() => new AspectAttribute(interceptor: typeof(object), explicitArgs: null));
            Assert.Throws<ArgumentNullException>(() => new AspectAttribute(factory: null));
        }

        [Test]
        public void Aspects_ApplyingAspectsShouldBeSequential([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var mockService = new Mock<IOrderInspectingService>(MockBehavior.Strict);
            mockService
                .Setup(x => x.GetAspectsOrder())
                .Returns(Array.Empty<string>());

            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(PooledLifetime.POOL_SCOPE);

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(Compiler);
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Factory(i => mockService.Object, lifetime);

            AbstractServiceEntry lastEntry = Collection.Last();
            lastEntry.Build(mockBuildContext.Object, [new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor()]);
            Compiler.Compile();

            IOrderInspectingService svc = (IOrderInspectingService) lastEntry.CreateInstance(mockInjector.Object, out object _);

            Assert.That(svc.GetAspectsOrder().SequenceEqual(
            [
                nameof(OrderInspectingAspect1Attribute),
                nameof(OrderInspectingAspect2Attribute),
                nameof(OrderInspectingAspect3Attribute)
            ]));
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

    public class MyGenericService<T> : IMyGenericService<T>, IMyGenericServiceHavingAspect<T> { }

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
            public object Invoke(IInvocationContext context, CallNextDelegate<IInvocationContext, object> callNext) => callNext(context);
        }

        public DummyAspectAttribute() : base(typeof(DummyInterceptor)) { }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class DummyAspectHavingDependencyAttribute : AspectAttribute
    {
        private sealed class DummyInterceptorHavingDependency : IInterfaceInterceptor
        {
            public DummyInterceptorHavingDependency(IDisposable dep) { }

            public object Invoke(IInvocationContext context, CallNextDelegate<IInvocationContext, object> callNext) => callNext(context);
        }

        public DummyAspectHavingDependencyAttribute() : base(typeof(DummyInterceptorHavingDependency)) { }
    }

    public abstract class OrderInspectingInterceptorBase : IInterfaceInterceptor
    {
        public string Name { get; }

        public OrderInspectingInterceptorBase(string name) => Name = name;

        public object Invoke(IInvocationContext context, CallNextDelegate<IInvocationContext, object> callNext)
        {
            return GetNames();

            IEnumerable<string> GetNames()
            {
                yield return Name;

                foreach (string name in (IEnumerable<string>) callNext(context))
                {
                    yield return name;
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect1Attribute : AspectAttribute
    {
        private sealed class OrderInspectingInterceptor : OrderInspectingInterceptorBase
        {
            public OrderInspectingInterceptor() : base(nameof(OrderInspectingAspect1Attribute)) { }
        }

        public OrderInspectingAspect1Attribute() : base(typeof(OrderInspectingInterceptor)) { }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect2Attribute : AspectAttribute
    {
        private sealed class OrderInspectingInterceptor : OrderInspectingInterceptorBase
        {
            public OrderInspectingInterceptor() : base(nameof(OrderInspectingAspect2Attribute)) { }
        }

        public OrderInspectingAspect2Attribute() : base(typeof(OrderInspectingInterceptor)) { }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class OrderInspectingAspect3Attribute : AspectAttribute
    {
        private sealed class OrderInspectingInterceptor : OrderInspectingInterceptorBase
        {
            public OrderInspectingInterceptor() : base(nameof(OrderInspectingAspect3Attribute)) { }
        }

        public OrderInspectingAspect3Attribute() : base(typeof(OrderInspectingInterceptor)) { }
    }

    [OrderInspectingAspect1, OrderInspectingAspect2, OrderInspectingAspect3]
    public interface IOrderInspectingService
    {
        IEnumerable<string> GetAspectsOrder();
    }
}
