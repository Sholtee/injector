/********************************************************************************
* Provider.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

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

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_MayHaveDeferredDependency(Lifetime lifetime) =>
            Assert.DoesNotThrow(() => Container.Provider<IInterface_1, ProviderHavingDeferredDependency>(lifetime));

        [Test]
        public void Container_Provider_ShouldCreateAProviderInstanceForEachService() 
        {
            AbstractServiceEntry entry = Container
                .Provider<IServiceProvider, SelfReturningProvider>(Lifetime.Transient)
                .Get<IServiceProvider>();

            Assert.That(entry.Factory(null, null), Is.InstanceOf<SelfReturningProvider>());
            Assert.AreNotSame(entry.Factory(null, entry.Interface), entry.Factory(null, entry.Interface));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Provider_ShouldSupportGenericServices(Lifetime lifetime) 
        {
            AbstractServiceEntry entry = Container
                .Provider(typeof(IList<>), typeof(TypeReturningProvider), lifetime)
                .Get<IList<int>>(QueryModes.AllowSpecialization);

            Assert.That(entry.Factory(null, entry.Interface), Is.EqualTo(typeof(IList<int>)));
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
    }
}
