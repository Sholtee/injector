/********************************************************************************
* Provider.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Internals;
    using Properties;
    
    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_Provider_ShouldThrowOnNonInterfaceKey() =>
            Assert.Throws<ArgumentException>(() => Container.Provider(typeof(object), typeof(DummyProvider)));

        [Test]
        public void Container_Provider_ShouldThrowIfTheProviderDoesNotImplementTheIServiceProvider() =>
            Assert.Throws<NotSupportedException>(() => Container.Provider(typeof(IInterface_1), typeof(object)));

        [Test]
        public void Container_Provider_ShouldThrowIfTheProviderHasNonInterfaceDependency() =>
            Assert.Throws<ArgumentException>(() => Container.Provider<IInterface_1, ProviderHavingNonInterfaceDependency>(), Resources.INVALID_CONSTRUCTOR);

        [Test]
        public void Container_Provider_ShouldSupportServiceActivatorAttribute() =>
            Assert.DoesNotThrow(() => Container.Provider<IInterface_1, ProviderHavingOverloadedCtor>());

        [Test]
        public void Container_Provider_ShouldBeAFactory() =>
            Assert.That(Container.Provider<IInterface_1, ProviderHavingOverloadedCtor>().Get<IInterface_1>().IsFactory());

        [Test]
        public void Container_Provider_MayHaveDeferredDependency() =>
            Assert.DoesNotThrow(() => Container.Provider<IInterface_1, ProviderHavingDeferredDependency>());

        [Test]
        public void Container_Provider_ShouldCreateAProviderInstanceForEachService() 
        {
            AbstractServiceEntry entry = Container
                .Provider<IServiceProvider, SelfReturningProvider>(Lifetime.Transient)
                .Get<IServiceProvider>();

            Assert.That(entry.Factory(null, null), Is.InstanceOf<SelfReturningProvider>());
            Assert.AreNotSame(entry.Factory(null, entry.Interface), entry.Factory(null, entry.Interface));
        }

        [Test]
        public void Container_Provider_ShouldSupportGenericServices() 
        {
            AbstractServiceEntry entry = Container
                .Provider(typeof(IList<>), typeof(TypeReturningProvider))
                .Get<IList<int>>(QueryModes.AllowSpecialization);

            Assert.That(entry.Factory(null, entry.Interface), Is.EqualTo(typeof(IList<int>)));
        }

        [Test]
        public void Container_Provider_ShouldThrowOnMultipleRegistration()
        {
            Container.Provider<IInterface_1, DummyProvider>();

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Provider<IInterface_1, DummyProvider>());
        }

        [Test]
        public void Container_Provider_CanBeRegisteredViaAttribute() 
        {
            Container.Setup(typeof(ConcreteProvider).Assembly, "Solti.Utils.DI.Container.Tests");

            AbstractServiceEntry entry = Container.Get<IDisposableEx>();

            Assert.IsNotNull(entry);
            Assert.That(entry.Lifetime, Is.EqualTo(Lifetime.Singleton));
            Assert.That(entry.Factory(new Mock<IInjector>(MockBehavior.Strict).Object, typeof(IDisposable)), Is.InstanceOf<Disposable>());
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

    [Provider(typeof(IDisposableEx), Lifetime.Singleton)]
    public class ConcreteProvider : IServiceProvider // ne nested legyen mert akkor generikusnak minosul (ContainerTestsBase<TContainer>.ConcreteProvider)
    {
        public object GetService(Type serviceType)
        {
            return new Disposable();
        }
    }
}
