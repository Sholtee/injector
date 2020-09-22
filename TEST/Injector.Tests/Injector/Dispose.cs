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

    public partial class InjectorTestsBase<TContainer>
    {
        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeOwnedEntries(Lifetime lifetime)
        {
            var mockSingleton = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockSingleton.Setup(s => s.Dispose());

            Container.Factory(inj => mockSingleton.Object, lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                injector.Get<IInterface_1_Disaposable>();
            }
           
            mockSingleton.Verify(s => s.Dispose(), Times.Once);
        }

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public async Task Injector_DisposeAsync_ShouldFreeOwnedEntriesAsynchronously(Lifetime lifetime)
        {
            var mockSingleton = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockSingleton
                .Setup(s => s.DisposeAsync())
                .Returns(default(ValueTask));

            Container.Factory(inj => mockSingleton.Object, lifetime);
#if LANG_VERSION_8
            await using (IInjector injector = Container.CreateInjector())
#else
            IInjector injector = Container.CreateInjector();
            try
#endif
            {
                injector.Get<IAsyncDisposable>();
            }
#if !LANG_VERSION_8
            finally { await injector.DisposeAsync(); }
#endif
            mockSingleton.Verify(s => s.DisposeAsync(), Times.Once);
        }

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeUsedLazyEntries(Lifetime lifetime) 
        {
            var disposable = new Disposable();

            Container
                .Factory<IDisposableEx>(i => disposable, lifetime)
                .Service<IInterface_7<Lazy<IDisposableEx>>, Implementation_7_TInterface_Dependant<Lazy<IDisposableEx>>>(lifetime);

            using (IInjector injector = Container.CreateInjector()) 
            {
                var svc = injector.Get<IInterface_7<Lazy<IDisposableEx>>>();
                Assert.That(svc.Interface, Is.Not.Null);              
            }

            Assert.That(disposable.Disposed, Is.False);

            using (IInjector injector = Container.CreateInjector())
            {
                var svc = injector.Get<IInterface_7<Lazy<IDisposableEx>>>();
                Assert.That(svc.Interface.Value, Is.Not.Null);
            }

            Assert.That(disposable.Disposed);
        }

        [TestCaseSource(nameof(InjectorControlledLifetimes))]
        public void Injector_Dispose_ShouldFreeEnumeratedServices(Lifetime lifetime)
        {
            Container.Service<IDisposableEx, Disposable>(lifetime);

            IDisposableEx svc;
            
            using (IInjector injector = Container.CreateInjector()) 
            {
                svc = injector.Get<IEnumerable<IDisposableEx>>().Single();
            }

            Assert.That(svc.Disposed);
        }
    }
}
