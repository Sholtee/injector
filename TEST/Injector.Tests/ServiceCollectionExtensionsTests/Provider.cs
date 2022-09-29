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
        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldThrowOnNonInterfaceKey(Lifetime lifetime) =>
            Assert.Throws<ArgumentException>(() => Collection.Provider(typeof(object), typeof(DummyProvider), lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldThrowIfTheProviderDoesNotImplementTheIServiceProvider(Lifetime lifetime) =>
            Assert.Throws<ArgumentException>(() => Collection.Provider(typeof(IInterface_1), typeof(object), lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldThrowIfTheProviderHasNonInterfaceDependency(Lifetime lifetime) =>
            Assert.Throws<ArgumentException>(() => Collection.Provider<IInterface_1, ProviderHavingNonInterfaceDependency>(lifetime), Resources.INVALID_CONSTRUCTOR);

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldSupportServiceActivatorAttribute(Lifetime lifetime) =>
            Assert.DoesNotThrow(() => Collection.Provider<IInterface_1, ProviderHavingOverloadedCtor>(lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldBeAService(Lifetime lifetime) =>
            Assert.That(Collection.Provider<IInterface_1, ProviderHavingOverloadedCtor>(lifetime).LastEntry.IsService());

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
            AbstractServiceEntry entry = Collection
                .Provider<IList, ListProvider>(lifetime)
                .LastEntry;
            entry.VisitFactory(_ => _, new SimpleDelegateCompiler());

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Lifetime)
                .Returns(new Mock<ILifetimeManager<object>>().Object);

            Assert.That(entry.CreateInstance(mockInjector.Object, out object _), Is.InstanceOf<IList>());
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Provider_ShouldSupportSpecializedServices(Lifetime lifetime)
        {
            AbstractServiceEntry entry = Collection
                .Provider<IList<int>, GenericListProvider>(lifetime)
                .LastEntry;
            entry.VisitFactory(_ => _, new SimpleDelegateCompiler());

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.Lifetime)
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
