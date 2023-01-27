/********************************************************************************
* Decorate.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Interfaces.Properties;
    using Internals;

    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Decorate_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionBasicExtensions.Decorate(null, (_, _, _) => new object()));
            Assert.Throws<ArgumentNullException>(() => Collection.Decorate(null));
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionAdvancedExtensions.Decorate<IInterfaceInterceptor>(null));
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public void Decorate_ShouldOverwriteTheFactoryFunction(Lifetime lifetime)
        {
            var mockCallback1 = new Mock<Func<IInjector, Type, object, object>>(MockBehavior.Strict);
            mockCallback1
                .Setup(_ => _(It.IsAny<IInjector>(), typeof(IInterface_1), It.Is<IInterface_1>(inst => inst is Implementation_1_No_Dep)))
                .Returns<IInjector, Type, object>((injector, type, inst) => inst);

            var mockCallback2 = new Mock<Func<IInjector, Type, object, object>>(MockBehavior.Strict);
            mockCallback2
                .Setup(_ => _(It.IsAny<IInjector>(), typeof(IInterface_1), It.Is<IInterface_1>(inst => inst is Implementation_1_No_Dep)))
                .Returns(new DecoratedImplementation_1());

            Collection
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime)
                .Decorate((injector, iface, curr) => mockCallback1.Object(injector, iface, curr))
                .Decorate((injector, iface, curr) => mockCallback2.Object(injector, iface, curr));

            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Options)
                .Returns(new ScopeOptions());

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Last().Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            Assert.That(Collection.Last().CreateInstance(mockInjector.Object, out _), Is.InstanceOf<DecoratedImplementation_1>());

            mockCallback1.Verify(_ => _(It.IsAny<IInjector>(), typeof(IInterface_1), It.IsAny<IInterface_1>()), Times.Once);
            mockCallback2.Verify(_ => _(It.IsAny<IInjector>(), typeof(IInterface_1), It.IsAny<IInterface_1>()), Times.Once);
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public void Decorate_ShouldWorkWithClosedGenerics(Lifetime lifetime)
        {
            var mockCallback = new Mock<Func<IInjector, Type, object, object>>(MockBehavior.Strict);
            mockCallback
                .Setup(_ => _(It.IsAny<IInjector>(), typeof(IInterface_3<int>), It.Is<IInterface_3<int>>(inst => inst is Implementation_3_IInterface_1_Dependant<int>)))
                .Returns(new DecoratedImplementation_3<int>());

            Collection
                .Service(typeof(IInterface_3<int>), typeof(Implementation_3_IInterface_1_Dependant<int>), lifetime)
                .Decorate((injector, iface, curr) => mockCallback.Object(injector, iface, curr));

            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            
            mockInjector
                .Setup(i => i.Get(It.Is<Type>(t => t == typeof(IInterface_1)), null))
                .Returns(new Implementation_1_No_Dep());

            mockInjector
                .SetupGet(i => i.Options)
                .Returns(new ScopeOptions());

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Last().Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            Assert.That(Collection.Last().CreateInstance(mockInjector.Object, out _), Is.InstanceOf<DecoratedImplementation_3<int>>());

            mockCallback.Verify(_ => _(It.IsAny<IInjector>(), typeof(IInterface_3<int>), It.IsAny<IInterface_3<int>>()), Times.Once);
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Decorate_ShouldThrowOnGenericService(Lifetime lifetime)
        {
            Collection.Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), lifetime);

            Assert.Throws<NotSupportedException>(() => Collection.Decorate((injector, type, inst) => inst), Resources.DECORATING_NOT_SUPPORTED);
        }

        [Test]
        public void Decorate_ShouldThrowOnInstances()
        {
            Collection.Instance<IInterface_1>(new Implementation_1_No_Dep());

            Assert.Throws<NotSupportedException>(() => Collection.Decorate((p1, p2, p3) => default), Resources.DECORATING_NOT_SUPPORTED);
        }

        [Test]
        public void Decorate_ShouldThrowOnAbstractService()
        {
            Collection.Register(new MissingServiceEntry(typeof(IInterface_1), null));

            Assert.Throws<NotSupportedException>(() => Collection.Decorate((p1, p2, p3) => default), Resources.DECORATING_NOT_SUPPORTED);
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public void Decorate_MayHaveDependency(Lifetime lifetime)
        {
            Collection
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(lifetime)
                .Decorate<MyInterceptorHavingDependency>();

            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);

            mockInjector
                .Setup(i => i.Get(typeof(IInterface_1), null))
                .Returns(new Implementation_1_No_Dep());

            mockInjector
                .Setup(i => i.Get(typeof(IInterface_3<int>), null))
                .Returns(new Implementation_3_IInterface_1_Dependant<int>(null));

            mockInjector
                .SetupGet(i => i.Options)
                .Returns(new ScopeOptions());

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Last().Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            object instance = Collection.Last().CreateInstance(mockInjector.Object, out _);

            Assert.That(instance, Is.InstanceOf<AspectAggregator<IInterface_2, IInterface_2>>());
            var proxy = (AspectAggregator<IInterface_2, IInterface_2>) instance;
            Assert.That(proxy.Interceptors.Count, Is.EqualTo(1));
            Assert.That(proxy.Interceptors[0], Is.InstanceOf<MyInterceptorHavingDependency>());
            var interceptor = (MyInterceptorHavingDependency) proxy.Interceptors[0];
            Assert.That(interceptor.Dependency, Is.InstanceOf<Implementation_3_IInterface_1_Dependant<int>>());
            Assert.That(proxy.Target, Is.InstanceOf<Implementation_2_IInterface_1_Dependant>());
            var original = (Implementation_2_IInterface_1_Dependant) proxy.Target;
            Assert.That(original.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());

            mockInjector.Verify(i => i.Get(typeof(IInterface_1), null), Times.Once);
            mockInjector.Verify(i => i.Get(typeof(IInterface_3<int>), null), Times.Once);
        }

        public class MyInterceptorHavingDependency : IInterfaceInterceptor
        {
            public MyInterceptorHavingDependency(IInterface_3<int> dependency)
            {
                Dependency = dependency;
            }

            public IInterface_3<int> Dependency { get; }

            public object Invoke(IInvocationContext context, InvokeInterceptorDelegate callNext) => callNext();
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public void Decorate_ShouldHandleNamedServices(Lifetime lifetime) 
        {
            Collection.Service<IInterface_1, Implementation_1_No_Dep>("cica", lifetime);
            Assert.DoesNotThrow(() => Collection.Decorate((p1, p2, p3) => new DecoratedImplementation_1()));

            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);

            mockInjector
                .SetupGet(i => i.Options)
                .Returns(new ScopeOptions());

            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            Collection.Last().Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });
            Assert.That(Collection.Last().CreateInstance(mockInjector.Object, out _), Is.InstanceOf<DecoratedImplementation_1>());
        }
    }
}
