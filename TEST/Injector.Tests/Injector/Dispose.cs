/********************************************************************************
* Dispose.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Primitives.Patterns;

    public partial class InjectorTests
    {
        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeServices(Lifetime lifetime)
        {
            var mockDisposable = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockDisposable.Setup(s => s.Dispose());

            Root = ScopeFactory.Create(svcs => svcs.Factory(inj => mockDisposable.Object, lifetime));

            using (IInjector injector = Root.CreateScope())
            {
                injector.Get<IInterface_1_Disaposable>();
            }
           
            mockDisposable.Verify(s => s.Dispose(), Times.Once);
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeServicesImplementingIAsyncDisposableInterfaceOnly(Lifetime lifetime)
        {
            var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockDisposable
                .Setup(s => s.DisposeAsync())
                .Returns(default(ValueTask));

            Root = ScopeFactory.Create(svcs => svcs.Factory(inj => mockDisposable.Object, lifetime));

            using (IInjector injector = Root.CreateScope())
            {
                injector.Get<IAsyncDisposable>();
            }

            mockDisposable.Verify(s => s.DisposeAsync(), Times.Once);
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public async Task Injector_DisposeAsync_ShouldFreeServicesAsynchronously(Lifetime lifetime)
        {
            var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockDisposable
                .Setup(s => s.DisposeAsync())
                .Returns(default(ValueTask));

            Root = ScopeFactory.Create(svcs => svcs.Factory(inj => mockDisposable.Object, lifetime));

            await using (IInjector injector = Root.CreateScope())
            {
                injector.Get<IAsyncDisposable>();
            }

            mockDisposable.Verify(s => s.DisposeAsync(), Times.Once);
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public async Task Injector_DisposeAsync_ShouldFreeServicesImplementingIDisposableAsynchronously(Lifetime lifetime)
        {
            var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
            mockDisposable.Setup(s => s.Dispose());

            Root = ScopeFactory.Create(svcs => svcs.Factory(inj => mockDisposable.Object, lifetime));

            await using (IInjector injector = Root.CreateScope())
            {
                injector.Get<IDisposable>();
            }

            mockDisposable.Verify(s => s.Dispose(), Times.Once);
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeUsedLazyServices(Lifetime lifetime) 
        {
            var disposable = new Disposable();

            Root = ScopeFactory.Create(svcs => svcs
                .Factory<IDisposableEx>(i => disposable, lifetime)
                .Service<IInterface_7<Lazy<IDisposableEx>>, Implementation_7_TInterface_Dependant<Lazy<IDisposableEx>>>(lifetime));

            using (IInjector injector = Root.CreateScope()) 
            {
                var svc = injector.Get<IInterface_7<Lazy<IDisposableEx>>>();
                Assert.That(svc.Dependency, Is.Not.Null);              
            }

            Assert.That(disposable.Disposed, Is.False);

            using (IInjector injector = Root.CreateScope())
            {
                var svc = injector.Get<IInterface_7<Lazy<IDisposableEx>>>();
                Assert.That(svc.Dependency.Value, Is.Not.Null);
            }

            Assert.That(disposable.Disposed);
        }

        [Test]
        public void Injector_Dispose_ShouldNotFreeInstances()
        {
            Disposable disposable = new();

            Root = ScopeFactory.Create(svcs => svcs.Instance<IDisposable>(disposable));

            using (IInjector injector = Root.CreateScope())
            {
                injector.Get<IDisposable>();
            }

            Assert.False(disposable.Disposed);

            Root.Dispose();
            Root = null;

            Assert.False(disposable.Disposed);
        }

        [TestCaseSource(nameof(ScopeControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeEnumeratedServices(Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IDisposableEx, MyDisposable>(lifetime));

            IDisposableEx svc;
            
            using (IInjector injector = Root.CreateScope()) 
            {
                svc = injector.Get<IEnumerable<IDisposableEx>>().Single();
            }

            Assert.That(svc.Disposed);
        }

        private class DisposableServiceNotifiesOnDispose : Disposable
        {
            private readonly Action<IDisposableEx> FOnDispose;

            public DisposableServiceNotifiesOnDispose(Action<IDisposableEx> onDispose) => FOnDispose = onDispose;

            protected override void Dispose(bool disposeManaged)
            {
                FOnDispose(this);
                base.Dispose(disposeManaged);
            }
        }

        [Test]
        public void Injector_Dispose_ShouldFreeServicesInAReverseOrder([ValueSource(nameof(ScopeControlledLifetimes))] Lifetime l1, [ValueSource(nameof(ScopeControlledLifetimes))] Lifetime l2, [ValueSource(nameof(ScopeControlledLifetimes))] Lifetime l3)
        {
            List<IDisposableEx> disposed = new();

            IDisposableEx
                s1,
                s2,
                s3;

            Root = ScopeFactory.Create(svcs => svcs
                .Factory<IDisposableEx>("0", _ => new DisposableServiceNotifiesOnDispose(disposed.Add), l1)
                .Factory<IDisposableEx>("1", _ => new DisposableServiceNotifiesOnDispose(disposed.Add), l2)
                .Factory<IDisposableEx>("2", _ => new DisposableServiceNotifiesOnDispose(disposed.Add), l3));

            using (IInjector injector = Root.CreateScope())
            {
                s1 = injector.Get<IDisposableEx>("0");
                s2 = injector.Get<IDisposableEx>("1");
                s3 = injector.Get<IDisposableEx>("2");
            }

            Assert.That(s1.Disposed);
            Assert.That(s2.Disposed);
            Assert.That(s3.Disposed);
            Assert.That(disposed[0], Is.SameAs(s3));
            Assert.That(disposed[1], Is.SameAs(s2));
            Assert.That(disposed[2], Is.SameAs(s1));
        }

        [Test]
        public void Injector_Dispose_MayLetServicesAccessTheirDependenciesOnDispose([ValueSource(nameof(Lifetimes))] Lifetime dependency, [ValueSource(nameof(Lifetimes))] Lifetime dependant, [Values(true, false)] bool dependencyAlreadyRequested)
        {
            IInterface_7<IDisposableEx> inst;

            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs
                .Service<IDisposableEx, MyDisposable>(dependency)
                .Service<IInterface_7<IDisposableEx>, Implementation_7<IDisposableEx>>(dependant)))
            {
                if (dependencyAlreadyRequested)
                {
                    using (IInjector injector = root.CreateScope())
                    {
                        injector.Get<IDisposableEx>();
                    }
                }

                using (IInjector effectiveInjector = root.CreateScope())
                {
                    inst = effectiveInjector.Get<IInterface_7<IDisposableEx>>();
                }
            }

            Assert.That(inst.Dependency.Disposed);
        }

        [Test]
        public void Injector_Dispose_ShouldBeControlledByDisposalMode([Values(typeof(IDisposableEx), typeof(IInterface_1))] Type iface, [ValueSource(nameof(Lifetimes))] Lifetime lifetime, [Values(ServiceDisposalMode.Soft, ServiceDisposalMode.Force, ServiceDisposalMode.Suppress)] ServiceDisposalMode disposalMode)
        {
            DisposableService inst;

            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs.Service(iface, typeof(DisposableService), lifetime, ServiceOptions.Default with { DisposalMode = disposalMode })))
            {
                using (IInjector injector = root.CreateScope())
                {
                    inst = (DisposableService) injector.Get(iface);
                }
            }

            Assert.That
            (
                inst.Disposed,
                Is.EqualTo
                (
                    disposalMode switch
                    {
                        ServiceDisposalMode.Soft => typeof(IDisposable).IsAssignableFrom(iface),
                        ServiceDisposalMode.Force => true,
                        ServiceDisposalMode.Suppress => false,
                        _ => throw new NotSupportedException()
                    }
                )
            );
        }
    }
}
