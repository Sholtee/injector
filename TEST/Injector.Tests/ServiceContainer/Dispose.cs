/********************************************************************************
* Dispose.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    public abstract partial class ServiceContainerTestsBase<TImplementation>
    {
        [Test]
        public void IServiceContainer_Dispose_ShouldDisposeOwnedEntriesOnly()
        {
            var mockOwned = new Mock<IDisposable>(MockBehavior.Strict);
            mockOwned.Setup(d => d.Dispose());

            var mockNotOwned = new Mock<IDisposable>(MockBehavior.Strict);
            mockNotOwned.Setup(d => d.Dispose());

            using (IServiceContainer container = new TImplementation())
            {
                container.Add(new InstanceServiceEntry(typeof(IDisposable), "owned", mockOwned.Object, externallyOwned: false, owner: container));
                container.Add(new InstanceServiceEntry(typeof(IDisposable), "notOwned", mockNotOwned.Object, externallyOwned: false, owner: new TImplementation()));

                Assert.That(container.Count, Is.EqualTo(2));
            }

            mockNotOwned.Verify(d => d.Dispose(), Times.Never);
            mockOwned.Verify(d => d.Dispose(), Times.Once);
        }

        [Test]
        public async Task IServiceContainer_DisposeAsync_ShouldDisposeOwnedEntriesOnly()
        {
            var mockOwned = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockOwned
                .Setup(d => d.DisposeAsync())
                .Returns(default(ValueTask));

            var mockNotOwned = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockNotOwned
                .Setup(d => d.DisposeAsync())
                .Returns(default(ValueTask));

            await using (IServiceContainer container = new TImplementation())
            {
                container.Add(new InstanceServiceEntry(typeof(IAsyncDisposable), "owned", mockOwned.Object, externallyOwned: false, owner: container));
                container.Add(new InstanceServiceEntry(typeof(IAsyncDisposable), "notOwned", mockNotOwned.Object, externallyOwned: false, owner: new TImplementation()));

                Assert.That(container.Count, Is.EqualTo(2));
            }

            mockNotOwned.Verify(d => d.DisposeAsync(), Times.Never);
            mockOwned.Verify(d => d.DisposeAsync(), Times.Once);
        }

        [Test]
        public void IServiceContainer_Dispose_ShouldDisposeChildContainerAndItsEntries()
        {
            IServiceContainer grandChild;
            IDisposable instance;

            using (IServiceContainer child = Container.CreateChild())
            {
                grandChild = child.CreateChild().Instance(instance = new Disposable(), releaseOnDispose: true);
            }

            Assert.Throws<ObjectDisposedException>(grandChild.Dispose);
            Assert.Throws<ObjectDisposedException>(instance.Dispose);
        }

        [Test]
        public async Task IServiceContainer_DisposeAsync_ShouldDisposeChildContainerAndItsEntries()
        {
            IServiceContainer grandChild;
            IDisposable instance;

            await using (IServiceContainer child = Container.CreateChild())
            {
                grandChild = child.CreateChild().Instance(instance = new Disposable(), releaseOnDispose: true);
            }

            Assert.Throws<ObjectDisposedException>(grandChild.Dispose);
            Assert.Throws<ObjectDisposedException>(instance.Dispose);
        }

        [Test]
        public void IServiceContainer_Dispose_ShouldDisposeSpecializedEntries()
        {
            Disposable testDisposable;

            using (IServiceContainer child = Container.CreateChild())
            {
                child
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                    .Service(typeof(IGenericDisposable<>), typeof(GenericDisposable<>), Lifetime.Singleton);

                Assert.That(child.Count, Is.EqualTo(2));
                Assert.AreSame(child.CreateInjector().Get<IGenericDisposable<int>>(), child.CreateInjector().Get<IGenericDisposable<int>>());
                Assert.That(child.Count, Is.EqualTo(3));

                testDisposable = (Disposable) child.CreateInjector().Get<IGenericDisposable<int>>();
                Assert.That(testDisposable.Disposed, Is.False);
            }

            Assert.That(testDisposable.Disposed, Is.True);
        }

        [Test]
        public async Task IServiceContainer_DisposeAsync_ShouldDisposeSpecializedEntries()
        {
            Disposable testDisposable;

            await using (IServiceContainer child = Container.CreateChild())
            {
                child
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                    .Service(typeof(IGenericDisposable<>), typeof(GenericDisposable<>), Lifetime.Singleton);

                Assert.That(child.Count, Is.EqualTo(2));
                Assert.AreSame(child.CreateInjector().Get<IGenericDisposable<int>>(), child.CreateInjector().Get<IGenericDisposable<int>>());
                Assert.That(child.Count, Is.EqualTo(3));

                testDisposable = (Disposable) child.CreateInjector().Get<IGenericDisposable<int>>();
                Assert.That(testDisposable.Disposed, Is.False);
            }

            Assert.That(testDisposable.Disposed, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void IServiceContainer_Dispose_ShouldFreeInstancesIfReleaseOnDisposeWasSetToTrue(bool releaseOnDispose)
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

            mockInstance.Verify(i => i.Dispose(), releaseOnDispose ? Times.Once : (Func<Times>)Times.Never);
        }

        public interface IGenericDisposable<T> : IDisposable
        {
        }

        public class GenericDisposable<T> : Disposable, IGenericDisposable<T>
        {
        }

        private sealed class BadDisposable: Disposable
        {
            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged) throw new Exception();
            }
        }
    }
}
