/********************************************************************************
* Dispose.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Internals;

    [TestFixture]
    public sealed partial class ContainerTests
    {
        [Test]
        public void Container_DisposeShouldFreeSingletonEntries()
        {
            var mockSingleton = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockSingleton.Setup(s => s.Dispose());

            var mockTransient = new Mock<IInterface_2_Disaposable>(MockBehavior.Strict);
            mockTransient.Setup(t => t.Dispose());

            using (IServiceContainer child = Container.CreateChild())
            {
                //
                // Register
                //

                child
                    .Factory(inj => mockSingleton.Object, Lifetime.Singleton)
                    .Factory(inj => mockTransient.Object, Lifetime.Transient);

                //
                // Use
                //

                IInjector childInjector = child.CreateInjector();

                childInjector.Get<IInterface_1_Disaposable>();
                childInjector.Get<IInterface_2_Disaposable>();
            }
           
            mockSingleton.Verify(s => s.Dispose(), Times.Once);
            mockTransient.Verify(t => t.Dispose(), Times.Never);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Container_DisposeShouldFreeInstancesIfReleaseOnDisposeWasSetToTrue(bool releaseOnDispose)
        {
            var mockInstance = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockInstance.Setup(i => i.Dispose());

            using (IServiceContainer child = Container.CreateChild())
            {
                child.Instance(mockInstance.Object, releaseOnDispose);

                using (child.CreateChild())
                {
                }

                mockInstance.Verify(i => i.Dispose(), Times.Never);
            }

            mockInstance.Verify(i => i.Dispose(), releaseOnDispose ? Times.Once : (Func<Times>) Times.Never);
        }

        [Test]
        public void Container_ShouldKeepUpToDateTheChildrenList()
        {
            Assert.That(Container.Children, Is.Empty);

            using (IServiceContainer child = Container.CreateChild())
            {
                Assert.That(Container.Children.Count, Is.EqualTo(1));
                Assert.AreSame(Container.Children.First(), child);
            }

            Assert.That(Container.Children, Is.Empty);
        }

        [Test]
        public void Container_DisposeShouldDisposeChildContainerAndItsEntries()
        {
            IServiceContainer grandChild;
            IDisposable singleton;
            
            using (IServiceContainer child = Container.CreateChild())
            {
                grandChild = child.CreateChild().Service<IDisposable, Disposable>(Lifetime.Singleton);
                singleton  = grandChild.CreateInjector().Get<IDisposable>();
            }

            var err = Assert.Throws<InvalidOperationException>(grandChild.Dispose);
            Assert.AreSame(err, Disposable.AlreadyDisposedException);

            err = Assert.Throws<InvalidOperationException>(singleton.Dispose);
            Assert.AreSame(err, Disposable.AlreadyDisposedException);
        }
    }
}
