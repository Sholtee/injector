/********************************************************************************
* RegisterKnownGenericServices.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.ServiceCollection.Tests
{
    using Interfaces;
    using Internals;

    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void RegisterKnownGenericServices_ShouldRegisterGenericServicesByCtorParameters()
        {
            ServiceCollection services = new();
            services
                .Service(typeof(IEnumerable<>), typeof(ServiceEnumerator<>), Lifetime.Scoped)
                .Service<IInterface_7<IEnumerable<IInjector>>, Implementation_7_TInterface_Dependant<IEnumerable<IInjector>>>(Lifetime.Scoped);

            Assert.That(services.Count, Is.EqualTo(2));

            services.RegisterKnownGenericServices();

            Assert.That(services.Count, Is.EqualTo(3));
            Assert.That(services.Contains(new ScopedServiceEntry(typeof(IEnumerable<IInjector>), null, typeof(ServiceEnumerator<IInjector>), null)));
        }

        [Test]
        public void RegisterKnownGenericServices_ShouldSupportNestedGenerics()
        {
            ServiceCollection services = new();
            services
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Scoped)
                .Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), Lifetime.Scoped)
                .Service(typeof(IInterface_6<>), typeof(Implementation_6_IInterface_3_Dependant<>), Lifetime.Scoped)
                .Service<IInterface_7<IInterface_6<string>>, Implementation_7_TInterface_Dependant<IInterface_6<string>>>(Lifetime.Scoped);

            Assert.That(services.Count, Is.EqualTo(4));

            services.RegisterKnownGenericServices();

            Assert.That(services.Count, Is.EqualTo(6));
            Assert.That(services.Contains(new ScopedServiceEntry(typeof(IInterface_6<string>), null, typeof(Implementation_6_IInterface_3_Dependant<string>), null)));
            Assert.That(services.Contains(new ScopedServiceEntry(typeof(IInterface_3<string>), null, typeof(Implementation_3_IInterface_1_Dependant<string>), null)));
        }

        [Test]
        public void RegisterKnownGenericServices_ShouldRegisterGenericServicesOnlyOnce()
        {
            ServiceCollection services = new();

            services
                .Service(typeof(IEnumerable<>), typeof(ServiceEnumerator<>), Lifetime.Scoped)
                .Service<IInterface_7<IEnumerable<IInjector>>, Implementation_7_TInterface_Dependant<IEnumerable<IInjector>>>(Lifetime.Scoped)
                .Service<IInterface_7<IEnumerable<IInjector>>, Implementation_7_TInterface_Dependant<IEnumerable<IInjector>>>("cica", Lifetime.Scoped);

            Assert.That(services.Count, Is.EqualTo(3));

            services.RegisterKnownGenericServices();

            Assert.That(services.Count, Is.EqualTo(4));
            Assert.That(services.Contains(new ScopedServiceEntry(typeof(IEnumerable<IInjector>), null, typeof(ServiceEnumerator<IInjector>), null)));
        }

        [Test]
        public void RegisterKnownGenericServices_ShouldIgnoreMissingServices()
        {
            ServiceCollection services = new();
            services.Service<IInterface_7<IEnumerable<IInjector>>, Implementation_7_TInterface_Dependant<IEnumerable<IInjector>>>(Lifetime.Scoped);

            Assert.DoesNotThrow(services.RegisterKnownGenericServices);
            Assert.That(services.Count, Is.EqualTo(1));
        }
    }
}
