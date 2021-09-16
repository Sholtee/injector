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
        private interface IService 
        {
            IInjector Scope { get; }
        }

        private class Service : IService
        {
            public Service(IInjector scope) => Scope = scope;

            public IInjector Scope { get; }
        }


        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldLock()
        {
            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs.Factory<IService>(i =>
                {
                    Assert.That(Monitor.IsEntered(i.Get<IServiceRegistry>().GetEntry<IService>()));
                    return new Service(i);
                }, Lifetime.Singleton)))
            {
                IInjector injector = root.CreateScope();

                injector.Get<IService>();

                AbstractServiceEntry entry = injector.Get<IServiceRegistry>().GetEntry<IService>();

                Assert.That(entry.State.HasFlag(ServiceEntryStates.Instantiated)); // factory hivva volt
                Assert.That(Monitor.IsEntered(entry), Is.False);
            }
        }

        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldInstantiateWithANewInjector()
        {
            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs.Service<IService, Service>(Lifetime.Singleton)))
            {
                IInjector injector = root.CreateScope();

                Assert.That(injector.Get<IService>().Scope, Is.Not.SameAs(injector));
            }
        }

        [Test]
        public void OwnedServiceInstantiationStrategy_ShouldUseTheExistingInjector()
        {
            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs.Service<IService, Service>(Lifetime.Transient)))
            {
                IInjector injector = root.CreateScope();

                Assert.That(injector.Get<IService>().Scope, Is.SameAs(injector));
            }
        }
    }
}
