/********************************************************************************
* Dispose.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Injector_DisposeShouldFreeScopedEntries()
        {
            var mockSingleton = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockSingleton.Setup(s => s.Dispose());

            Container.Factory(inj => mockSingleton.Object, Lifetime.Scoped);

            using (IInjector injector = Container.CreateInjector())
            {
                injector.Get<IInterface_1_Disaposable>();
            }
           
            mockSingleton.Verify(s => s.Dispose(), Times.Once);
        }

        [Test]
        public void Injector_DisposeShouldFreeTransientEntries() 
        {
            var obj = new Mock<IEnumerator<string>>(MockBehavior.Strict);
            obj.Setup(t => t.Dispose());

            Container.Factory(i => obj.Object, Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector()) 
            {
                injector.Get<IEnumerator<string>>();
            }

            obj.Verify(t => t.Dispose(), Times.Once);
        }
    }
}
