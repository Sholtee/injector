/********************************************************************************
* GetService.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Annotations;

    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void ServiceProvider_GetService_ShouldResolveItself1() 
        {
            using (Container.CreateProvider(out var provider)) 
            {
                Assert.That(provider.GetService<IServiceProvider>(), Is.EqualTo(provider));            
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldResolveItself2()
        {
            Container.Service<IInterface_7<IServiceProvider>, Implementation_7_TInterface_Dependant<IServiceProvider>>();

            using (Container.CreateProvider(out var provider))
            {
                var svc = provider.GetService<IInterface_7<IServiceProvider>>();

                Assert.That(svc.Interface, Is.EqualTo(provider));
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldReturnNullOnMissingService1() 
        {
            using (Container.CreateProvider(out var provider))
            {
                Assert.That(provider.GetService<IInterface_1>(), Is.Null);
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldReturnNullOnMissingService2([Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime)
        {
            Container.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime);

            using (Container.CreateProvider(out var provider))
            {
                var svc = provider.GetService<IInterface_7<IInterface_1>>();

                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.Null);
            }
        }

        private sealed class MyServiceUsingNamedDependency : IInterface_7<IInterface_1>
        {
            public MyServiceUsingNamedDependency([Options(Name = "cica")]IInterface_1 dep) => Interface = dep;

            public IInterface_1 Interface { get; }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldResolveNamedDependencies() 
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>("cica")
                .Service<IInterface_7<IInterface_1>, MyServiceUsingNamedDependency>();

            using (Container.CreateProvider(out var provider))
            {
                var svc = provider.GetService<IInterface_7<IInterface_1>>();

                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldResolveDependencies()
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>();

            using (Container.CreateProvider(out var provider))
            {
                var svc = provider.GetService<IInterface_7<IInterface_1>>();

                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }
    }
}
