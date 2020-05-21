/********************************************************************************
* Dispose.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;

    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void ServiceProvider_Dispose_ShouldFreeOwnedEntries([Values(Lifetime.Scoped, Lifetime.Transient)] Lifetime lifetime)
        {
            var mockSingleton = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockSingleton.Setup(s => s.Dispose());

            Container.Factory(inj => mockSingleton.Object, lifetime);

            using (Container.CreateProvider(out IServiceProvider provider))
            {
                provider.GetService<IInterface_1_Disaposable>();
            }

            mockSingleton.Verify(s => s.Dispose(), Times.Once);
        }

        [Test]
        public async Task ServiceProvider_DisposeAsync_ShouldFreeOwnedEntriesAsynchronously([Values(Lifetime.Scoped, Lifetime.Transient)] Lifetime lifetime)
        {
            var mockSingleton = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockSingleton
                .Setup(s => s.DisposeAsync())
                .Returns(default(ValueTask));

            Container.Factory(inj => mockSingleton.Object, lifetime);
#if LANG_VERSION_8
            await using (Container.CreateProvider(out IServiceProvider provider))
#else
            IAsyncDisposable scope = Container.CreateProvider(out IServiceProvider provider);
            try
#endif
            {
                provider.GetService<IAsyncDisposable>();
            }
#if !LANG_VERSION_8
            finally { await scope.DisposeAsync(); }
#endif
            mockSingleton.Verify(s => s.DisposeAsync(), Times.Once);
        }
    }
}
