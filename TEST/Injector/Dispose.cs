/********************************************************************************
* Dispose.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Internals;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void Injector_DisposeShouldFreeScopedEntries()
        {
            var mockSingleton = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockSingleton.Setup(s => s.Dispose());

            var mockTransient = new Mock<IInterface_2_Disaposable>(MockBehavior.Strict);
            mockTransient.Setup(t => t.Dispose());

            Container
                .Factory(inj => mockSingleton.Object, Lifetime.Scoped)
                .Factory(inj => mockTransient.Object, Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector())
            {
                injector.Get<IInterface_1_Disaposable>();
                injector.Get<IInterface_2_Disaposable>();
            }
           
            mockSingleton.Verify(s => s.Dispose(), Times.Once);
            mockTransient.Verify(t => t.Dispose(), Times.Never);
        }
    }
}
