/********************************************************************************
* Provider.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
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
    }
}
