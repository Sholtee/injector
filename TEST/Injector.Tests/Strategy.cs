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

    public class ServiceInstantiationStrategyTests
    {
        private interface IService { }
        private class Service : IService { }

        private interface IService_2 { }

        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldLock()
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                container.Factory<IService>(i =>
                {
                    Assert.That(Monitor.IsEntered(container.Get<IService>()));
                    return new Service();
                }, Lifetime.Singleton);

                container.CreateInjector().Get<IService>();

                Assert.That(container.Get<IService>().Instances, Is.Not.Null); // factory hivva volt
                Assert.That(Monitor.IsEntered(container.Get<IService>()), Is.False);
            }
        }

        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldInstantiateWithANewInjector()
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                container.Service<IService, Service>(Lifetime.Singleton);

                IInjector injector = container.CreateInjector();

                injector.Get<IService>();

                AbstractServiceEntry entry = injector.UnderlyingContainer.Get<IService>();

                Assert.That(entry.Instances.Single().RelatedInjector, Is.Not.EqualTo(injector));
                Assert.That(entry.Instances.Single().RelatedInjector.UnderlyingContainer.Parent, Is.EqualTo(entry.Owner));
            }
        }

        [Test]
        public void OwnedServiceInstantiationStrategy_ShouldUseTheExistingInjector()
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                container.Service<IService, Service>(Lifetime.Transient);

                IInjector injector = container.CreateInjector();

                injector.Get<IService>();

                AbstractServiceEntry entry = injector.UnderlyingContainer.Get<IService>();

                Assert.That(entry.Instances.Single().RelatedInjector, Is.EqualTo(injector));
                Assert.That(entry.Instances.Single().RelatedInjector.UnderlyingContainer, Is.EqualTo(entry.Owner));
            }
        }
    }
}
