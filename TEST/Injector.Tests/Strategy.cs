/********************************************************************************
* Strategy.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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
