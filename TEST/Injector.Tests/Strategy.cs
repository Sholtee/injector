/********************************************************************************
* Strategy.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Threading;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;

    using ScopeFactory = DI.ScopeFactory;

    public class ServiceInstantiationStrategyTests
    {
        private interface IService { }
        private class Service : IService { }

        private interface IService_2 { }

        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldLock()
        {
            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs.Factory<IService>(i =>
                {
                    Assert.That(Monitor.IsEntered(i.Get<IServiceRegistry>().GetEntry<IService>()));
                    return new Service();
                }, Lifetime.Singleton)))
            {
                IInjector injector = root.CreateScope();

                injector.Get<IService>();

                AbstractServiceEntry entry = injector.Get<IServiceRegistry>().GetEntry<IService>();

                Assert.That(entry.Instances, Is.Not.Empty); // factory hivva volt
                Assert.That(Monitor.IsEntered(entry), Is.False);
            }
        }

        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldInstantiateWithANewInjector()
        {
            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs.Service<IService, Service>(Lifetime.Singleton)))
            {
                IInjector injector = root.CreateScope();

                injector.Get<IService>();

                Assert.That(injector.Get<IServiceRegistry>().GetEntry<IService>().Instances.Single().Scope, Is.Not.SameAs(injector));
            }
        }

        [Test]
        public void OwnedServiceInstantiationStrategy_ShouldUseTheExistingInjector()
        {
            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs.Service<IService, Service>(Lifetime.Transient)))
            {
                IInjector injector = root.CreateScope();

                injector.Get<IService>();

                Assert.That(injector.Get<IServiceRegistry>().GetEntry<IService>().Instances.Single().Scope, Is.SameAs(injector));
            }
        }
    }
}
