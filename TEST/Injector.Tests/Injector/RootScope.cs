/********************************************************************************
* RootScope.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;

    public partial class InjectorTests
    {
        [Test]
        public void Injector_Create_ShouldThrowOnNonRegisteredDependency([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            var ex = Assert.Throws<ServiceNotFoundException>(() => ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime), new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.AOT }));

            Assert.That(ex.Data["requested"], Is.InstanceOf<MissingServiceEntry>().And.EqualTo(new MissingServiceEntry(typeof(IInterface_1), null)).Using(ServiceIdComparer<AbstractServiceEntry>.Instance));
            Assert.That(ex.Data["requestor"], Is.EqualTo(new DummyServiceEntry(typeof(IInterface_7<IInterface_1>), null)).Using(ServiceIdComparer<AbstractServiceEntry>.Instance));
        }

        [Test]
        public void Injector_Create_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2) => Assert.Throws<CircularReferenceException>(() => ScopeFactory.Create
        (
            svcs => svcs
                .Service<IInterface_4, Implementation_4_CDep>(lifetime1)
                .Service<IInterface_5, Implementation_5_CDep>(lifetime2),
            new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.AOT }
        ), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));

        [Test]
        public void Injector_CreateUsingFactory_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2) => Assert.Throws<CircularReferenceException>(() => ScopeFactory.Create
        (
            svcs => svcs
                .Factory<IInterface_4>(injector => new Implementation_4_CDep(injector.Get<IInterface_5>(null)), lifetime1)
                .Factory<IInterface_5>(injector => new Implementation_5_CDep(injector.Get<IInterface_4>(null)), lifetime2),
            new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.AOT }
        ), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));

        [Test]
        public void Injector_CreateUsingCtor_ShouldThrowOnCircularReference() => Assert.Throws<CircularReferenceException>(() => ScopeFactory.Create
        (
            svcs => svcs
                .Service<IInterface_1, Implementation_7_CDep>(Lifetime.Transient)
                .Service<IInterface_4, Implementation_4_CDep>(Lifetime.Transient)
                .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Transient),
            new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.AOT }
        ), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));

        [Test]
        public void Injector_CreateUsingProvider_ShouldThrowOnCircularReference() => Assert.Throws<CircularReferenceException>(() => ScopeFactory.Create
        (
            svcs => svcs
                .Service<IInterface_4, Implementation_4_CDep>(Lifetime.Transient)
                .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Transient)
                .Provider<IInterface_1, CdepProvider>(Lifetime.Transient),
            new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.AOT }
        ), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));

        [Test]
        public void Injector_CreateUsingDecorator_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime) => Assert.Throws<CircularReferenceException>(() => ScopeFactory.Create
        (
            svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime).Decorate((injector, _, _) => injector.Get<IInterface_1>(null)),
            new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.AOT }
        ), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
    }
}
