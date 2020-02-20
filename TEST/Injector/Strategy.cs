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

namespace Solti.Utils.DI.Injector.Tests
{
    using Internals;

    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void InstanceStrategy_ShouldBeUsedWhenThereIsAnInstanceAssigned()
        {
            var entry = new InstanceServiceEntry(typeof(IDisposable), null, new Disposable(), true, Container);

            IServiceInstantiationStrategy strategy = new InstanceStrategy();
            Assert.That(strategy.ShouldUse(null, entry));
        }

        [Test]
        public void OwnedServiceInstantiationStrategy_ShouldBeUsedWhenTheEntryIsOwned() 
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector()) 
            {
                IServiceInstantiationStrategy strategy = new OwnedServiceInstantiationStrategy();
                Assert.That(strategy.ShouldUse(injector, injector.UnderlyingContainer.Get<IInterface_1>()));
            }
        }

        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldBeUsedWhenTheEntryIsNotOwned()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Singleton);

            using (IInjector injector = Container.CreateInjector())
            {
                IServiceInstantiationStrategy strategy = new NotOwnedServiceInstantiationStrategy();
                Assert.That(strategy.ShouldUse(injector, injector.UnderlyingContainer.Get<IInterface_1>()));
            }
        }

        [Test]
        public void OwnedServiceInstantiationStrategy_ShouldInstantiateTheDependency()
        {
            var requested = new TransientServiceEntry(typeof(IInterface_1), null, typeof(Implementation_1_No_Dep), Container);

            var mockInjector = new Mock<IStatefulInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Instantiate(It.Is<ServiceReference>(sr => sr.RelatedInjector == mockInjector.Object && sr.RelatedServiceEntry == requested)));

            IServiceInstantiationStrategy strategy = new OwnedServiceInstantiationStrategy();

            Assert.DoesNotThrow(() => strategy.Exec(mockInjector.Object, null, requested));

            mockInjector.Verify(i => i.Instantiate(It.Is<ServiceReference>(sr => sr.RelatedInjector == mockInjector.Object && sr.RelatedServiceEntry == requested)), Times.Once);
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
            var instance = new Implementation_1_No_Dep();
            var requested = new InstanceServiceEntry(typeof(IInterface_1), null, instance, false, Container);

            var mockInjector = new Mock<IStatefulInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Instantiate(It.IsAny<ServiceReference>()));

            var requestor = new ServiceReference(new AbstractServiceEntry(typeof(IInterface_2), null));

            IServiceInstantiationStrategy strategy = (IServiceInstantiationStrategy) para;

            ServiceReference dep = strategy.Exec(mockInjector.Object, requestor, requested);

            Assert.That(dep.Value, Is.EqualTo(instance));
            mockInjector.Verify(i => i.Instantiate(It.IsAny<ServiceReference>()), Times.Never);
            Assert.That(requestor.Dependencies.Single(), Is.EqualTo(dep));
        }

        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldLock()
        {
            Container.Factory<IInterface_1>(i => 
            {
                Assert.That(Monitor.IsEntered(Container.Get<IInterface_1>()));
                return new Implementation_1_No_Dep();
            }, Lifetime.Singleton);

            var requestor = new ServiceReference(new AbstractServiceEntry(typeof(IInterface_2), null));

            IServiceInstantiationStrategy strategy = new NotOwnedServiceInstantiationStrategy();

            strategy.Exec(new Injector(Container), requestor, Container.Get<IInterface_1>());

            Assert.That(Container.Get<IInterface_1>().Instance, Is.Not.Null); // factory hivva volt
            Assert.That(Monitor.IsEntered(Container.Get<IInterface_1>()), Is.False);
        }



        [Test]
        public void NotOwnedServiceInstantiationStrategy_ShouldInstantiateWithANewInjector()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Singleton);

            var requestor = new ServiceReference(new AbstractServiceEntry(typeof(IInterface_2), null));

            IServiceInstantiationStrategy strategy = new NotOwnedServiceInstantiationStrategy();

            using (var injector = new Injector(Container))
            {
                strategy.Exec(injector, requestor, Container.Get<IInterface_1>());

                AbstractServiceEntry entry = Container.Get<IInterface_1>();

                Assert.That(entry.Instance.RelatedInjector, Is.Not.EqualTo(injector));
                Assert.That(entry.Instance.RelatedInjector.UnderlyingContainer.Parent, Is.EqualTo(entry.Owner));
            }
        }
    }
}
