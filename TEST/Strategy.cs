/********************************************************************************
* Strategy.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Internals;
    using Properties;

    public class ServiceInstantiationStrategyTests
    {
        private interface IService { }
        private class Service : IService { }
        private interface IService_2 { }

        [Test]
        public void InstanceStrategy_ShouldBeUsedWhenThereIsAnInstanceAssigned()
        {
            var entry = new InstanceServiceEntry(typeof(IDisposable), null, new Disposable(), true, new ServiceContainer());

            IServiceInstantiationStrategy strategy = new InstanceStrategy();
            Assert.That(strategy.ShouldUse(null, entry));
        }

        [Test]
        public void OwnedServiceInstantiationStrategy_ShouldBeUsedWhenTheEntryIsOwned() 
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                container.Service<IService, Service>(Lifetime.Transient);

                var injector = new Injector(container); // nem kell using (a szulovel egyutt felszabadul)

                IServiceInstantiationStrategy strategy = new OwnedServiceInstantiationStrategy();
                Assert.That(strategy.ShouldUse(injector, injector.UnderlyingContainer.Get<IService>()));
            }
        }

        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldBeUsedWhenTheEntryIsNotOwned()
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                container.Service<IService, Service>(Lifetime.Singleton);

                var injector = new Injector(container);

                IServiceInstantiationStrategy strategy = new NotOwnedServiceInstantiationStrategy();
                Assert.That(strategy.ShouldUse(injector, injector.UnderlyingContainer.Get<IService>()));
            }
        }

        [Test]
        public void OwnedServiceInstantiationStrategy_ShouldInstantiateTheDependency()
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                var requested = new TransientServiceEntry(typeof(IService), null, typeof(Service), container);

                var mockInjector = new Mock<Injector>(() => new Injector(container, null));

                IServiceInstantiationStrategy strategy = new OwnedServiceInstantiationStrategy();

                Assert.DoesNotThrow(() => strategy.Exec(mockInjector.Object, null, requested));

                mockInjector.Verify(i => i.Instantiate(It.Is<ServiceReference>(sr => sr.RelatedInjector == mockInjector.Object && sr.RelatedServiceEntry == requested)), Times.Once);

                container.Children.Remove(mockInjector.Object); // hack
            }
        }

        internal static IEnumerable<IServiceInstantiationStrategy> Strategies 
        {
            get 
            {
                yield return new InstanceStrategy();
                yield return new OwnedServiceInstantiationStrategy();
                yield return new NotOwnedServiceInstantiationStrategy();
            }
        }

        [TestCaseSource(nameof(Strategies))]
        public void ServiceInstantiationStrategy_ShouldSetTheReturnedInstanceAsDependency(object para)
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                var instance = new Service();
                var requested = new InstanceServiceEntry(typeof(IService), null, instance, false, container);

                var mockInjector = new Mock<Injector>(() => new Injector(container, null));

                var requestor = new ServiceReference(new AbstractServiceEntry(typeof(IService_2), null), new Injector(container));

                IServiceInstantiationStrategy strategy = (IServiceInstantiationStrategy) para;

                ServiceReference dep = strategy.Exec(mockInjector.Object, requestor, requested);

                Assert.That(dep.Value, Is.EqualTo(instance));
                mockInjector.Verify(i => i.Instantiate(It.IsAny<ServiceReference>()), Times.Never);
                Assert.That(requestor.Dependencies.Single(), Is.EqualTo(dep));

                container.Children.Remove(mockInjector.Object); // hack
            }
        }

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

                var requestor = new ServiceReference(new AbstractServiceEntry(typeof(IService_2), null), new Injector(container));

                IServiceInstantiationStrategy strategy = new NotOwnedServiceInstantiationStrategy();

                strategy.Exec(new Injector(container), requestor, container.Get<IService>());

                Assert.That(container.Get<IService>().Instance, Is.Not.Null); // factory hivva volt
                Assert.That(Monitor.IsEntered(container.Get<IService>()), Is.False);
            }
        }

        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldInstantiateWithANewInjector()
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                container.Service<IService, Service>(Lifetime.Singleton);

                var requestor = new ServiceReference(new AbstractServiceEntry(typeof(IService_2), null), new Injector(container));

                IServiceInstantiationStrategy strategy = new NotOwnedServiceInstantiationStrategy();

                var injector = new Injector(container);

                strategy.Exec(injector, requestor, container.Get<IService>());

                AbstractServiceEntry entry = container.Get<IService>();

                Assert.That(entry.Instance.RelatedInjector, Is.Not.EqualTo(injector));
                Assert.That(entry.Instance.RelatedInjector.UnderlyingContainer.Parent, Is.EqualTo(entry.Owner));
            }
        }

        [Test]
        public void StrategySelector_ShouldThrowIfThereIsNoCompatibleStrategy()
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                Assert.Throws<InvalidOperationException>(() 
                    => new ServiceInstantiationStrategySelector(new Injector(container)).GetStrategyFor(new AbstractServiceEntry(typeof(IService), null)),
                Resources.NO_STRATEGY);
            }
        }
    }
}
