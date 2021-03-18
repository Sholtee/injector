/********************************************************************************
* Provider.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Interfaces;
    using Properties;
    
    public partial class ContainerTestsBase<TContainer>
    {
        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_ShouldThrowOnNonInterfaceKey(Lifetime lifetime) =>
            Assert.Throws<ArgumentException>(() => Container.Provider(typeof(object), typeof(DummyProvider), lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_ShouldThrowIfTheProviderDoesNotImplementTheIServiceProvider(Lifetime lifetime) =>
            Assert.Throws<ArgumentException>(() => Container.Provider(typeof(IInterface_1), typeof(object), lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_ShouldThrowIfTheProviderHasNonInterfaceDependency(Lifetime lifetime) =>
            Assert.Throws<ArgumentException>(() => Container.Provider<IInterface_1, ProviderHavingNonInterfaceDependency>(lifetime), Resources.INVALID_CONSTRUCTOR);

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_ShouldSupportServiceActivatorAttribute(Lifetime lifetime) =>
            Assert.DoesNotThrow(() => Container.Provider<IInterface_1, ProviderHavingOverloadedCtor>(lifetime));

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_ShouldBeAFactory(Lifetime lifetime) =>
            Assert.That(Container.Provider<IInterface_1, ProviderHavingOverloadedCtor>(lifetime).Get<IInterface_1>().IsFactory());

        public static IEnumerable<(Lifetime Actual, Lifetime Underlying)> UnderlyingLifetimes 
        {
            get 
            {
                yield return (Lifetime.Transient, Lifetime.Transient);
                yield return (Lifetime.Scoped, Lifetime.Transient);
                yield return (Lifetime.Pooled, Lifetime.Transient);
                yield return (Lifetime.Singleton, Lifetime.Singleton); // StrictDI tamogatas
            }
        }

        [Test]
        public void Container_Provider_UnderlyingProviderShouldHaveTheProperLifetime([ValueSource(nameof(UnderlyingLifetimes))] (Lifetime Actual, Lifetime Underlying) para) 
        {
            Container.Provider<IList<int>, ListProvider>(para.Actual);

            Assert.That(Container.Get<IList<int>>().Lifetime, Is.EqualTo(para.Actual));
            Assert.That(Container.Get<IServiceProvider>($"{ServiceContainer.INTERNAL_SERVICE_NAME_PREFIX}{typeof(IList<int>).FullName}_provider_").Lifetime, Is.EqualTo(para.Underlying));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_MayHaveDeferredDependency(Lifetime lifetime) =>
            Assert.DoesNotThrow(() => Container.Provider<IInterface_1, ProviderHavingDeferredDependency>(lifetime));

        [Test]
        public void Container_Provider_ShouldCreateAProviderInstanceForEachService() 
        {
            AbstractServiceEntry entry = Container
                .Provider<IServiceProvider, SelfReturningProvider>(Lifetime.Transient)
                .Get<IServiceProvider>();

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IServiceProvider), $"{ServiceContainer.INTERNAL_SERVICE_NAME_PREFIX}{typeof(IServiceProvider).FullName}_provider_"))
                .Returns<Type, string>((t, n) => Container.Get(t, n).Factory(null, t));

            Assert.That(entry.Factory(mockInjector.Object, entry.Interface), Is.InstanceOf<SelfReturningProvider>());
            Assert.AreNotSame(entry.Factory(mockInjector.Object, entry.Interface), entry.Factory(mockInjector.Object, entry.Interface));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_ShouldSupportGenericServices(Lifetime lifetime) 
        {
            AbstractServiceEntry entry = Container
                .Provider(typeof(IList<>), typeof(ListProvider), lifetime)
                .Get<IList<int>>(QueryModes.AllowSpecialization);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IServiceProvider), $"{ServiceContainer.INTERNAL_SERVICE_NAME_PREFIX}{typeof(IList<>).FullName}_provider_"))
                .Returns<Type, string>((t, n) => Container.Get(t, n).Factory(null, t));

            Assert.That(entry.Factory(mockInjector.Object, typeof(IList<int>)), Is.InstanceOf<IList<int>>());

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Once);
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_ShouldSupportSpecializedServices(Lifetime lifetime)
        {
            AbstractServiceEntry entry = Container
                .Provider(typeof(IList<>), typeof(FaultyProvider), lifetime)
                .Provider<IList<int>, ListProvider>(lifetime)
                .Get<IList<int>>(QueryModes.AllowSpecialization);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IServiceProvider), It.IsAny<string>()))
                .Returns<Type, string>((t, n) => Container.Get(t, n).Factory(null, t));

            Assert.That(entry.Factory(mockInjector.Object, typeof(IList<int>)), Is.InstanceOf<IList<int>>());

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Once);
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_ShouldThrowOnMultipleRegistration(Lifetime lifetime)
        {
            Container.Provider<IInterface_1, DummyProvider>(lifetime);

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Provider<IInterface_1, DummyProvider>(lifetime));
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

        private class SelfReturningProvider : DummyProvider 
        {
            public override object GetService(Type serviceType) => this;
        }

        private class TypeReturningProvider : DummyProvider
        {
            public override object GetService(Type serviceType) => serviceType;
        }

        private class FaultyProvider : IServiceProvider
        {
            public object GetService(Type serviceType) => throw new NotImplementedException();
        }

        private class ListProvider : IServiceProvider
        {
            public object GetService(Type serviceType) => Activator.CreateInstance
            (
                typeof(List<>).MakeGenericType(serviceType.GetGenericArguments().Single()),
                Type.EmptyTypes
            );
        }
    }
}
