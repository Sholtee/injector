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

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;
    using Primitives.Patterns;

    public partial class InjectorTests
    {
        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeOwnedEntries(Lifetime lifetime)
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

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public async Task Injector_DisposeAsync_ShouldFreeOwnedEntriesAsynchronously(Lifetime lifetime)
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

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeUsedLazyEntries(Lifetime lifetime) 
        {
            var disposable = new Disposable();

            Root = ScopeFactory.Create(svcs => svcs
                .Factory<IDisposableEx>(i => disposable, lifetime)
                .Service<IInterface_7<Lazy<IDisposableEx>>, Implementation_7_TInterface_Dependant<Lazy<IDisposableEx>>>(lifetime));

            using (IInjector injector = Root.CreateScope()) 
            {
                var svc = injector.Get<IInterface_7<Lazy<IDisposableEx>>>();
                Assert.That(svc.Interface, Is.Not.Null);              
            }

            Assert.That(disposable.Disposed, Is.False);

            using (IInjector injector = Root.CreateScope())
            {
                var svc = injector.Get<IInterface_7<Lazy<IDisposableEx>>>();
                Assert.That(svc.Interface.Value, Is.Not.Null);
            }

            Assert.That(disposable.Disposed);
        }

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeEnumeratedServices(Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IDisposableEx, Disposable>(lifetime));

            IDisposableEx svc;
            
            using (IInjector injector = Root.CreateScope()) 
            {
                svc = injector.Get<IEnumerable<IDisposableEx>>().Single();
            }

            Assert.That(svc.Disposed);
        }
    }
}
