/********************************************************************************
* GetService.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;

    public partial class InjectorTests
    {
        [Test]
        public void ServiceProvider_GetService_ShouldResolveItself1() 
        {
            Root = ScopeFactory.Create(svcs => { }, new ScopeOptions { SupportsServiceProvider = true });

            using (Root.CreateScope(out IServiceProvider provider)) 
            {
                Assert.That(provider.GetService<IServiceProvider>(), Is.EqualTo(provider));            
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldResolveItself2()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs.Service<IInterface_7<IServiceProvider>, Implementation_7_TInterface_Dependant<IServiceProvider>>(Lifetime.Transient), 
                new ScopeOptions { SupportsServiceProvider = true }
            );

            using (Root.CreateScope(out IServiceProvider provider))
            {
                IInterface_7<IServiceProvider> svc = provider.GetService<IInterface_7<IServiceProvider>>();

                Assert.That(svc.Interface, Is.EqualTo(provider));
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldReturnNullOnMissingService1() 
        {
            Root = ScopeFactory.Create(svcs => { }, new ScopeOptions { SupportsServiceProvider = true });

            using (Root.CreateScope(out IServiceProvider provider))
            {
                Assert.That(provider.GetService<IInterface_1>(), Is.Null);
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldReturnNullOnMissingService2([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime),
                new ScopeOptions { SupportsServiceProvider = true }
            );

            using (Root.CreateScope(out IServiceProvider provider))
            {
                IInterface_7<IInterface_1> svc = provider.GetService<IInterface_7<IInterface_1>>();

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
        public void ServiceProvider_GetService_ShouldResolveNamedDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2) 
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>("cica", lifetime1)
                    .Service<IInterface_7<IInterface_1>, MyServiceUsingNamedDependency>(lifetime2),
                new ScopeOptions { SupportsServiceProvider = true }
            );

            using (Root.CreateScope(out IServiceProvider provider))
            {
                var svc = provider.GetService<IInterface_7<IInterface_1>>();

                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldResolveDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(lifetime1)
                    .Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime2),
                new ScopeOptions { SupportsServiceProvider = true }
            );

            using (Root.CreateScope(out IServiceProvider provider))
            {
                var svc = provider.GetService<IInterface_7<IInterface_1>>();

                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }
    }
}
