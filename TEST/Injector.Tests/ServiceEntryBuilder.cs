﻿/********************************************************************************
* ServiceEntryBuilder.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;
    using Primitives.Threading;

    [TestFixture]
    public sealed class ServiceEntryBuilderTests
    {
        public abstract class DummyInjector : IInjector, IInstanceFactory
        {
            public IInstanceFactory Super { get; }
            public object Lifetime { get; } = new Mock<ILifetimeManager<object>>().Object;
            public ScopeOptions Options { get; }
            public bool Disposed { get; }
            public void Dispose() { }
            public ValueTask DisposeAsync() => default;

            public abstract object CreateInstance(AbstractServiceEntry requested);
            public abstract object Get(Type iface, string name = null);
            public abstract object GetOrCreateInstance(AbstractServiceEntry requested, int slot);
            public abstract object TryGet(Type iface, string name = null);
        }

        private interface IMyService
        {
        }

        private class MyService : IMyService
        {
            [ServiceActivator]
            public MyService(IList dep) { }

            public MyService(IList dep, int extra) { }
        }

        private class MyService_GenericDep : IMyService
        {
            [ServiceActivator]
            public MyService_GenericDep(IList<int> dep) { }

            public MyService_GenericDep(IList<int> dep, int extra) { }
        }

        public IScopeFactory Root { get; set; }

        public static IEnumerable<Lifetime> Lifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
                yield return Lifetime.Singleton;
                yield return Lifetime.Pooled;
            }
        }

        static ServiceEntryBuilderTests() =>
            //
            // Lifetime.XxX properties are late bound so we need to initialize them (outside the
            // test environment it is done when the InjectorDotNet library is loaded).
            //

            InjectorDotNetLifetime.Initialize();

        [TearDown]
        public void TearDwon()
        {
            Root?.Dispose();
            Root = null;
        }

        private void DoTest(IModifiedServiceCollection svcs, ServiceResolutionMode resolutionMode, Lifetime lifetime)
        {
            AbstractServiceEntry dependant = svcs.LastEntry;

            svcs = svcs.Factory<IList>(_ => new List<object>(), lifetime);

            AbstractServiceEntry dependency = svcs.LastEntry;

            Mock<DummyInjector> mockInjector = new(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList), null))
                .Returns(new List<object>());
            mockInjector
                .Setup(i => i.CreateInstance(dependency))
                .Returns(new List<object>());
            mockInjector
                .Setup(i => i.GetOrCreateInstance(dependency, It.IsAny<int>()))
                .Returns(new List<object>());

            Root = ScopeFactory.Create
            (
                svcs,
                new ScopeOptions
                {
                    ServiceResolutionMode = resolutionMode
                }
            );

            Assert.That(dependant.State.HasFlag(ServiceEntryStateFlags.Built));

            dependant.CreateInstance(mockInjector.Object, out _);

            if (resolutionMode is ServiceResolutionMode.JIT)
            {
                mockInjector.Verify(i => i.Get(typeof(IList), null), Times.Once);
                mockInjector.Verify(i => i.CreateInstance(It.IsAny<AbstractServiceEntry>()), Times.Never);
                mockInjector.Verify(i => i.GetOrCreateInstance(It.IsAny<AbstractServiceEntry>(), It.IsAny<int>()), Times.Never);
            }
            else if (resolutionMode is ServiceResolutionMode.AOT)
            {
                mockInjector.Verify(i => i.CreateInstance(dependency), Times.Exactly(!dependency.Features.HasFlag(ServiceEntryFlags.CreateSingleInstance) ? 1 : 0));
                mockInjector.Verify(i => i.GetOrCreateInstance(dependency, It.IsAny<int>()), Times.Exactly(dependency.Features.HasFlag(ServiceEntryFlags.CreateSingleInstance) ? 1 : 0));
                mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
            }
            else Assert.Fail("Unknown resolution mode");
        }

        private sealed class MyList<T> : List<T> { } // just to have one public ctor

        private void DoGenericTest(IModifiedServiceCollection svcs, ServiceResolutionMode resolutionMode, Lifetime lifetime)
        {
            AbstractServiceEntry dependant = svcs.LastEntry;

            svcs = svcs.Service(typeof(IList<>), typeof(MyList<>), lifetime);

            AbstractServiceEntry dependency = svcs.LastEntry;

            Mock<DummyInjector> mockInjector = new(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<int>), null))
                .Returns(new List<int>());
            mockInjector
                .Setup(i => i.CreateInstance(It.Is<AbstractServiceEntry>(se => se.Interface == typeof(IList<int>))))
                .Returns(new List<int>());
            mockInjector
                .Setup(i => i.GetOrCreateInstance(It.Is<AbstractServiceEntry>(se => se.Interface == typeof(IList<int>)), It.IsAny<int>()))
                .Returns(new List<int>());

            Root = ScopeFactory.Create
            (
                svcs,
                new ScopeOptions
                {
                    ServiceResolutionMode = resolutionMode
                }
            );

            Assert.That(dependant.State.HasFlag(ServiceEntryStateFlags.Built));

            dependant.CreateInstance(mockInjector.Object, out _);

            if (resolutionMode is ServiceResolutionMode.JIT)
            {
                mockInjector.Verify(i => i.Get(typeof(IList<int>), null), Times.Once);
                mockInjector.Verify(i => i.CreateInstance(It.IsAny<AbstractServiceEntry>()), Times.Never);
                mockInjector.Verify(i => i.GetOrCreateInstance(It.IsAny<AbstractServiceEntry>(), It.IsAny<int>()), Times.Never);
            }
            else if (resolutionMode is ServiceResolutionMode.AOT)
            {
                mockInjector.Verify(i => i.CreateInstance(It.Is<AbstractServiceEntry>(se => se.Interface == typeof(IList<int>))), Times.Exactly(!dependency.Features.HasFlag(ServiceEntryFlags.CreateSingleInstance) ? 1 : 0));
                mockInjector.Verify(i => i.GetOrCreateInstance(It.Is<AbstractServiceEntry>(se => se.Interface == typeof(IList<int>)), It.IsAny<int>()), Times.Exactly(dependency.Features.HasFlag(ServiceEntryFlags.CreateSingleInstance) ? 1 : 0));
                mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
            }
            else Assert.Fail("Unknown resolution mode");
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_ServiceExt([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection() 
                .Service<IMyService, MyService>(Lifetime.Scoped);

            DoTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_Service([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Service(typeof(IMyService), typeof(MyService), Lifetime.Scoped);

            DoTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_ServiceArbitraryParamExt([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Service<IMyService, MyService>(new { extra = 0 }, Lifetime.Scoped);

            DoTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_ServiceArbitraryParam([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Service(typeof(IMyService), typeof(MyService), new { extra = 0 }, Lifetime.Scoped);

            DoTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_FactoryExt([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Factory<IMyService>(injector => new MyService(injector.Get<IList>(null)), Lifetime.Scoped);

            DoTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_Factory([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Factory(typeof(IMyService), (injector, _) => new MyService(injector.Get<IList>(null)), Lifetime.Scoped);

            DoTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_ServiceExt_Generic([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Service<IMyService, MyService_GenericDep>(Lifetime.Scoped);

            DoGenericTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_Service_Generic([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Service(typeof(IMyService), typeof(MyService_GenericDep), Lifetime.Scoped);

            DoGenericTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_ServiceArbitraryParamExt_Generic([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Service<IMyService, MyService_GenericDep>(new { extra = 0 }, Lifetime.Scoped);

            DoGenericTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_ServiceArbitraryParam_Generic([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Service(typeof(IMyService), typeof(MyService_GenericDep), new { extra = 0 }, Lifetime.Scoped);

            DoGenericTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_FactoryExt_Generic([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Factory<IMyService>(injector => new MyService_GenericDep(injector.Get<IList<int>>(null)), Lifetime.Scoped);

            DoGenericTest(svcs, resolutionMode, lifetime);
        }

        [Test]
        public void Builder_MayModifyInjectorInvocations_Factory_Generic([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            IModifiedServiceCollection svcs = new ServiceCollection()
                .Factory(typeof(IMyService), (injector, _) => new MyService_GenericDep(injector.Get<IList<int>>(null)), Lifetime.Scoped);

            DoGenericTest(svcs, resolutionMode, lifetime);
        }
    }
}