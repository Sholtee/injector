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
    using Interfaces;
    using Internals;
    using Primitives.Patterns;
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
                var requested = new SingletonServiceEntry(typeof(IService), null, typeof(Service), container);

                IServiceInstantiationStrategy strategy = new OwnedServiceInstantiationStrategy();

                IServiceReference @ref = null;

                Assert.DoesNotThrow(() => @ref = strategy.Exec(new Injector(container, null), requested));
                Assert.That(@ref.Value, Is.InstanceOf<Service>());
                Assert.That(@ref.RefCount, Is.EqualTo(1));
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
        public void ServiceInstantiationStrategy_ShouldSetTheReturnedInstanceAsADependency(object para)
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                var instance = new Service();
                var requested = new InstanceServiceEntry(typeof(IService), null, instance, false, container);

                var mockInjector = new Mock<Injector>(() => new Injector(container, null));

                var requestor = new ServiceReference(new AbstractServiceEntry(typeof(IService_2), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Injector(container));

                IServiceReference dep = ServiceInstantiationStrategySelector.GetStrategyInvocation((IServiceInstantiationStrategy) para, mockInjector.Object, requested).Invoke(requestor);

                Assert.That(dep.Value, Is.EqualTo(instance));
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

                IServiceInstantiationStrategy strategy = new NotOwnedServiceInstantiationStrategy();

                strategy.Exec(new Injector(container), container.Get<IService>());

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

                IServiceInstantiationStrategy strategy = new NotOwnedServiceInstantiationStrategy();

                var injector = new Injector(container);

                strategy.Exec(injector, container.Get<IService>());

                AbstractServiceEntry entry = container.Get<IService>();

                Assert.That(entry.Instances.Single().RelatedInjector, Is.Not.EqualTo(injector));
                Assert.That(entry.Instances.Single().RelatedInjector.UnderlyingContainer.Parent, Is.EqualTo(entry.Owner));
            }
        }

        [Test]
        public void StrategySelector_ShouldThrowIfThereIsNoCompatibleStrategy()
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                Assert.Throws<InvalidOperationException>(()
                    //
                    // Ne "new Mock<IServiceContainer>(MockBehavior.Strict).Object" et hasznaljunk
                    //

                    => ServiceInstantiationStrategySelector.GetStrategyFor(new Injector(container), new AbstractServiceEntry(typeof(IService), null, new ServiceContainer())),
                Resources.NO_STRATEGY);
            }
        }
    }
}
