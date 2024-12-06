/********************************************************************************
* Dispose.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;

    public partial class InjectorTests
    {
        [Test]
        public void ServiceProvider_Dispose_ShouldFreeOwnedEntries([ValueSource(nameof(ScopeControlledLifetimes))] Lifetime lifetime)
        {
            var mockSingleton = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockSingleton.Setup(s => s.Dispose());

            Root = ScopeFactory.Create
            (
                svcs => svcs.Factory(inj => mockSingleton.Object, lifetime),
                new ScopeOptions { SupportsServiceProvider = true }
            );

            using (Root.CreateScope(out IServiceProvider provider))
            {
                provider.GetService<IInterface_1_Disaposable>();
            }

            mockSingleton.Verify(s => s.Dispose(), Times.Once);
        }

        [Test]
        public async Task ServiceProvider_DisposeAsync_ShouldFreeOwnedEntriesAsynchronously([ValueSource(nameof(ScopeControlledLifetimes))] Lifetime lifetime)
        {
            var mockSingleton = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockSingleton
                .Setup(s => s.DisposeAsync())
                .Returns(default(ValueTask));

            Root = ScopeFactory.Create
            (
                svcs => svcs.Factory(inj => mockSingleton.Object, lifetime),
                new ScopeOptions { SupportsServiceProvider = true }
            );

            await using (Root.CreateScope(out IServiceProvider provider))
            {
                provider.GetService<IAsyncDisposable>();
            }

            mockSingleton.Verify(s => s.DisposeAsync(), Times.Once);
        }
    }
}
