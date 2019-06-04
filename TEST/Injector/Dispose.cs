/********************************************************************************
* Dispose.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Internals;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        public interface IInterface_1_Disaposable: IInterface_1, IDisposable
        {            
        }

        public interface IInterface_2_Disaposable : IInterface_2, IDisposable
        {
        }

        [Test]
        public void Injector_DisposeShouldFreeSingletonEntries()
        {
            var mockSingleton = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockSingleton.Setup(s => s.Dispose());

            var mockTransient = new Mock<IInterface_2_Disaposable>(MockBehavior.Strict);
            mockTransient.Setup(t => t.Dispose());

            using (IInjector child = Injector.CreateChild())
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

                child.Get<IInterface_1_Disaposable>();
                child.Get<IInterface_2_Disaposable>();
            }
           
            mockSingleton.Verify(s => s.Dispose(), Times.Once);
            mockTransient.Verify(t => t.Dispose(), Times.Never);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Injector_DisposeShouldFreeInstancesIfReleaseOnDisposeSetToTrue(bool releaseOnDispose)
        {
            var mockInstance = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockInstance.Setup(i => i.Dispose());

            using (IInjector child = Injector.CreateChild())
            {
                child
                    .Instance(mockInstance.Object, releaseOnDispose)
                    .Get<IInterface_1_Disaposable>();

                using (IInjector grandChild = child.CreateChild())
                {
                    grandChild.Get<IInterface_1_Disaposable>();       
                }

                mockInstance.Verify(i => i.Dispose(), Times.Never);
            }

            mockInstance.Verify(i => i.Dispose(), releaseOnDispose ? Times.Once : (Func<Times>) Times.Never);
        }

        [Test]
        public void Injector_ShouldKeepUpToDateTheChildrenList()
        {
            Assert.That(Injector.Children, Is.Empty);

            using (IInjector child = Injector.CreateChild())
            {
                Assert.That(Injector.Children.Count, Is.EqualTo(1));
                Assert.AreSame(Injector.Children.First(), child);
            }

            Assert.That(Injector.Children, Is.Empty);
        }

        [Test]
        public void Injector_DisposeShouldDisposeChildInjectorAndItsEntries()
        {
            IInjector grandChild;
            IDisposable singleton;
            
            using (IInjector child = Injector.CreateChild())
            {
                grandChild = child.CreateChild().Service<IDisposable, Disposable>(Lifetime.Singleton);
                singleton  = grandChild.Get<IDisposable>();
            }

            var err = Assert.Throws<InvalidOperationException>(grandChild.Dispose);
            Assert.AreSame(err, Disposable.AlreadyDisposedException);

            err = Assert.Throws<InvalidOperationException>(singleton.Dispose);
            Assert.AreSame(err, Disposable.AlreadyDisposedException);
        }

        [Test]
        public void Injector_ShouldNotDisposeInheritedInstances()
        {
            var mockInstance = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockInstance.Setup(i => i.Dispose());

            using (IInjector child = Injector.CreateChild())
            {
                child.Instance(mockInstance.Object, releaseOnDispose: true);

                using (IInjector grandChild = child.CreateChild())
                {
                    Assert.AreSame(mockInstance.Object, grandChild.Get<IInterface_1_Disaposable>());                   
                }

                mockInstance.Verify(i => i.Dispose(), Times.Never);
            }

            mockInstance.Verify(i => i.Dispose(), Times.Once);
        }
    }
}
