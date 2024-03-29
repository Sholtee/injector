﻿/********************************************************************************
* GetService.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;

    public partial class InjectorTests
    {
        [Test]
        public void ServiceProvider_GetService_ShouldResolveItself1([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode) 
        {
            Root = ScopeFactory.Create(svcs => { }, new ScopeOptions { SupportsServiceProvider = true, ServiceResolutionMode = resolutionMode });

            using (Root.CreateScope(out IServiceProvider provider)) 
            {
                Assert.That(provider.GetService<IServiceProvider>(), Is.EqualTo(provider));            
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldResolveItself2([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs.Service<IInterface_7<IServiceProvider>, Implementation_7_TInterface_Dependant<IServiceProvider>>(Lifetime.Transient), 
                new ScopeOptions { SupportsServiceProvider = true, ServiceResolutionMode = resolutionMode }
            );

            using (Root.CreateScope(out IServiceProvider provider))
            {
                IInterface_7<IServiceProvider> svc = provider.GetService<IInterface_7<IServiceProvider>>();

                Assert.That(svc.Dependency, Is.EqualTo(provider));
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldReturnNullOnMissingService1([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode) 
        {
            Root = ScopeFactory.Create(svcs => { }, new ScopeOptions { SupportsServiceProvider = true, ServiceResolutionMode = resolutionMode });

            using (Root.CreateScope(out IServiceProvider provider))
            {
                Assert.That(provider.GetService<IInterface_1>(), Is.Null);
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldReturnNullOnMissingService2([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime),
                new ScopeOptions { SupportsServiceProvider = true, ServiceResolutionMode = resolutionMode }
            );

            using (Root.CreateScope(out IServiceProvider provider))
            {
                IInterface_7<IInterface_1> svc = provider.GetService<IInterface_7<IInterface_1>>();

                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Dependency, Is.Null);
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldResolveKeyedDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode) 
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>("cica", lifetime1)
                    .Service<IInterface_7<IInterface_1>, ServiceHavingKeyedDependency>(lifetime2),
                new ScopeOptions { SupportsServiceProvider = true, ServiceResolutionMode = resolutionMode }
            );

            using (Root.CreateScope(out IServiceProvider provider))
            {
                var svc = provider.GetService<IInterface_7<IInterface_1>>();

                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Dependency, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShouldResolveDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(lifetime1)
                    .Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime2),
                new ScopeOptions { SupportsServiceProvider = true, ServiceResolutionMode = resolutionMode }
            );

            using (Root.CreateScope(out IServiceProvider provider))
            {
                var svc = provider.GetService<IInterface_7<IInterface_1>>();

                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Dependency, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void ServiceProvider_GetService_ShoulThrowOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceProviderBasicExtensions.GetService<IInterface_1>(null));
        }
    }
}
