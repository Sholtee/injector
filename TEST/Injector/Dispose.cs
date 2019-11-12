/********************************************************************************
* Dispose.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
            var obj1 = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            obj1.Setup(t => t.Dispose());

            var obj2 = new Mock<IEnumerator<string>>(MockBehavior.Strict);
            obj2.Setup(t => t.Dispose());

            Container
                .Factory(i => obj1.Object, Lifetime.Transient)
                .Factory(i => obj2.Object, Lifetime.Transient);

            IDisposable
                disposed,
                notDisposed;

            using (IInjector injector = Container.CreateInjector()) 
            {
                disposed = injector.Get<IInterface_1_Disaposable>();
                disposed.Dispose();

                notDisposed = injector.Get<IEnumerator<string>>();
            }

            obj1.Verify(t => t.Dispose(), Times.Once);
            obj2.Verify(t => t.Dispose(), Times.Once);
        }
    }
}
