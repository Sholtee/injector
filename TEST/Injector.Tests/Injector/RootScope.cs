/********************************************************************************
* RootScope.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

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

            Assert.That(ex.Requested, Is.EqualTo(new ServiceId(typeof(IInterface_1), null)).Using(IServiceId.Comparer.Instance));
            Assert.That(ex.Requestor, Is.EqualTo(new ServiceId(typeof(IInterface_7<IInterface_1>), null)).Using(IServiceId.Comparer.Instance));
        }

        [Test]
        public void Injector_Create_ShouldThrowOnInvalidDisposalMode([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Assert.Throws<NotSupportedException>
            (
                () => ScopeFactory.Create
                (
                    svcs => svcs.Service<IInterface_1, Implementation_1>(lifetime, ServiceOptions.Default with { DisposalMode = (ServiceDisposalMode) 1986 })
                )
            );
        }

        [Test]
        public void Injector_CreateScope_ShouldThrowOnDisposedRoot([Values(true, false)] bool supportsServiceProvider)
        {
            IScopeFactory root = ScopeFactory.Create(svcs => { }, ScopeOptions.Default with { SupportsServiceProvider = supportsServiceProvider });
            root.Dispose();

            Assert.Throws<ObjectDisposedException>(() => root.CreateScope());
        }

        [Test]
        public void Injector_Create_ShouldThrowOnInvalidResolutionMode([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Assert.Throws<NotSupportedException>
            (
                () => ScopeFactory.Create
                (
                    svcs => svcs.Service<IInterface_1, Implementation_1>(lifetime),
                    ScopeOptions.Default with { ServiceResolutionMode = (ServiceResolutionMode) 1986 }
                )
            );
        }

        [Test]
        public void Injector_Create_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => ScopeFactory.Create(registerServices: null));
            Assert.Throws<ArgumentNullException>(() => ScopeFactory.Create(services: null));
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
                .Factory<IInterface_4>(factoryExpr: injector => new Implementation_4_CDep(injector.Get<IInterface_5>(null)), lifetime1)
                .Factory<IInterface_5>(factoryExpr: injector => new Implementation_5_CDep(injector.Get<IInterface_4>(null)), lifetime2),
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
