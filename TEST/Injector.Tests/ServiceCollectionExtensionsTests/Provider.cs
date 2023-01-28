/********************************************************************************
* Provider.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Threading;
    using Properties;

    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Provider_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => Collection.Provider(null, typeof(DummyProvider), Lifetime.Singleton));
            Assert.Throws<ArgumentNullException>(() => Collection.Provider(null, typeof(DummyProvider), new object(), Lifetime.Singleton));
            Assert.Throws<ArgumentNullException>(() => Collection.Provider(typeof(ICloneable), typeof(DummyProvider), null));
            Assert.Throws<ArgumentNullException>(() => Collection.Provider(typeof(ICloneable), typeof(DummyProvider), new object(), null));
            Assert.Throws<ArgumentNullException>(() => Collection.Provider(typeof(ICloneable), null, Lifetime.Singleton));
            Assert.Throws<ArgumentNullException>(() => Collection.Provider(typeof(ICloneable), typeof(DummyProvider), null, Lifetime.Singleton));
            Assert.Throws<ArgumentNullException>(() => Collection.Provider(typeof(ICloneable), null, new object(), Lifetime.Singleton));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldThrowOnNonInterfaceKey(Lifetime lifetime)
        {
            Assert.Throws<ArgumentException>(() => Collection.Provider(typeof(object), typeof(DummyProvider), lifetime));
            Assert.Throws<ArgumentException>(() => Collection.Provider(typeof(object), typeof(DummyProvider), new object(), lifetime));
        }

        [DummyAspect]
        private sealed class ProviderHavingAspect : IServiceProvider
        {
            public object GetService(Type serviceType) => throw new NotImplementedException();
        }

        [Test]
        public void Provider_ShouldThrowOnImplementationHavingAspects()
        {
            Assert.Throws<NotSupportedException>(() => Collection.Provider<IList, ProviderHavingAspect>(Lifetime.Scoped), Interfaces.Properties.Resources.DECORATING_NOT_SUPPORTED);
            Assert.Throws<NotSupportedException>(() => Collection.Provider<IList, ProviderHavingAspect>(new object(), Lifetime.Scoped), Interfaces.Properties.Resources.DECORATING_NOT_SUPPORTED);
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldThrowIfTheProviderDoesNotImplementTheIServiceProvider(Lifetime lifetime)
        {
            Assert.Throws<ArgumentException>(() => Collection.Provider(typeof(IInterface_1), typeof(object), lifetime));
            Assert.Throws<ArgumentException>(() => Collection.Provider(typeof(IInterface_1), typeof(object), new object(), lifetime));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldThrowIfTheProviderHasNonInterfaceDependency(Lifetime lifetime) =>
            Assert.Throws<ArgumentException>(() => Collection.Provider<IInterface_1, ProviderHavingNonInterfaceDependency>(lifetime), Resources.INVALID_DEPENDENCY);

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldSupportServiceActivatorAttribute(Lifetime lifetime) =>
            Assert.DoesNotThrow(() => Collection.Provider<IInterface_1, ProviderHavingOverloadedCtor>(lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldBeAService(Lifetime lifetime) =>
            Assert.That(Collection.Provider<IInterface_1, ProviderHavingOverloadedCtor>(lifetime).Last().IsService());

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldSupportExplicitArgs(Lifetime lifetime) =>
            Assert.DoesNotThrow(() => Collection.Provider<IInterface_1, ProviderHavingNonInterfaceDependency>(new Dictionary<string, object> { ["val"] = 1 }, lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_MayHaveDeferredDependency(Lifetime lifetime) =>
            Assert.DoesNotThrow(() => Collection.Provider<IInterface_1, ProviderHavingDeferredDependency>(lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldNotSupportGenericServices(Lifetime lifetime) =>
            Assert.Throws<NotSupportedException>(() => Collection.Provider(typeof(IList<>), typeof(GenericListProvider), lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldSupportRegularServices(Lifetime lifetime)
        {
            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            AbstractServiceEntry entry = Collection
                .Provider<IList, ListProvider>(lifetime)
                .Last();

            entry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });

            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(new Mock<ILifetimeManager<object>>().Object);

            Assert.That(entry.CreateInstance(mockInjector.Object, out object _), Is.InstanceOf<IList>());
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldSupportSpecializedServices(Lifetime lifetime)
        {
            var mockBuildContext = new Mock<IBuildContext>(MockBehavior.Strict);
            mockBuildContext
                .SetupGet(ctx => ctx.Compiler)
                .Returns(new SimpleDelegateCompiler());
            mockBuildContext
                .Setup(ctx => ctx.AssignSlot())
                .Returns(0);

            AbstractServiceEntry entry = Collection
                .Provider<IList<int>, GenericListProvider>(lifetime)
                .Last();
            entry.Build(mockBuildContext.Object, new IFactoryVisitor[] { new MergeProxiesVisitor(), new ApplyLifetimeManagerVisitor() });

            var mockInjector = new Mock<IServiceActivator>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Tag)
                .Returns(new Mock<ILifetimeManager<object>>().Object);

            Assert.That(entry.CreateInstance(mockInjector.Object, out object _), Is.InstanceOf<IList<int>>());
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldThrowOnMultipleRegistration(Lifetime lifetime)
        {
            Collection.Provider<IInterface_1, DummyProvider>(lifetime);

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Provider<IInterface_1, DummyProvider>(lifetime));
        }

        private class DummyProvider : IServiceProvider
        {
            public virtual object GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }
        }

        private class ProviderHavingNonInterfaceDependency : DummyProvider 
        {
            public ProviderHavingNonInterfaceDependency(int val) { }
        }

        private class ProviderHavingOverloadedCtor : DummyProvider 
        {
            [ServiceActivator]
            public ProviderHavingOverloadedCtor() { }
            public ProviderHavingOverloadedCtor(int val) { }
        }

        private class ProviderHavingDeferredDependency : DummyProvider
        {
            public ProviderHavingDeferredDependency(IInterface_2 dep1, Lazy<IInterface_4> dep2) { }
        }

        private class ListProvider : IServiceProvider
        {
            public object GetService(Type serviceType) => new List<object>();
        }

        private class GenericListProvider : IServiceProvider
        {
            public object GetService(Type serviceType) => Activator.CreateInstance
            (
                typeof(List<>).MakeGenericType(serviceType.GetGenericArguments().Single()),
                Type.EmptyTypes
            );
        }
    }
}
