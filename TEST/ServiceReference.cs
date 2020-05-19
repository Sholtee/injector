/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Primitives.Patterns;
    using Properties;

    [TestFixture]
    public sealed class ServiceReferenceTests
    {
        public interface IDummyService { }

        [Test]
        public void Value_CanBeSetOnce() 
        {
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>(MockBehavior.Strict).Object)
            {
                Value = new Mock<IDummyService>().Object
            };

            Assert.Throws<InvalidOperationException>(() => svc.Value = new Mock<IDummyService>().Object);
        }

        [Test]
        public void SetValue_ShouldValidate() 
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>(MockBehavior.Strict).Object);

            Assert.Throws<ArgumentNullException>(() => reference.Value = null);
            Assert.Throws<InvalidOperationException>(() => reference.Value = new object(), Resources.INVALID_INSTANCE);
            Assert.DoesNotThrow(() => reference.Value = new Disposable());
        }

        [Test]
        public void Dispose_ShouldDisposeTheValueAndDecrementTheRefCountOfTheDependencies() 
        {         
            var dependency = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = new Mock<IDummyService>().Object // refcount == 1
            };

            var target = new Disposable();

            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = target 
            };

            svc.Dependencies.Add(dependency);

            Assert.That(dependency.RefCount, Is.EqualTo(2));

            svc.Release();

            Assert.That(target.Disposed);
            Assert.That(dependency.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposeAsync_ShouldDisposeTheValueSynchronouslyAndDecrementTheRefCountOfTheDependencies()
        {
            var dependency = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = new Mock<IDummyService>().Object 
            };

            var target = new Mock<IDisposable>(MockBehavior.Strict);

            target.Setup(d => d.Dispose());

            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = target.Object 
            };

            svc.Dependencies.Add(dependency);

            Assert.That(dependency.RefCount, Is.EqualTo(2));

            svc.DisposeAsync().AsTask().Wait();

            target.Verify(d => d.Dispose(), Times.Once);
            Assert.That(dependency.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposeAsync_ShouldDisposeTheValueAsynchronouslyAndDecrementTheRefCountOfTheDependencies()
        {
            var dependency = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = new Mock<IDummyService>().Object 
            };

            var target = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            target
                .Setup(d => d.DisposeAsync())
                .Returns(default(ValueTask));

            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IAsyncDisposable), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = target.Object 
            };

            svc.Dependencies.Add(dependency);

            Assert.That(dependency.RefCount, Is.EqualTo(2));

            svc.DisposeAsync().AsTask().Wait();

            target.Verify(d => d.DisposeAsync(), Times.Once);
            Assert.That(dependency.RefCount, Is.EqualTo(1));
        }

        public interface ICompositeDisposable : IDisposable, IAsyncDisposable { }

        [Test]
        public void DisposeAsync_AsynchronousDisposalShouldHaveHigherPriority()
        {
            var target = new Mock<ICompositeDisposable>(MockBehavior.Strict);
            target
                .Setup(d => d.DisposeAsync())
                .Returns(default(ValueTask));

            var svc = new ServiceReference(new AbstractServiceEntry(typeof(ICompositeDisposable), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = target.Object
            };

            svc.DisposeAsync().AsTask().Wait();

            target.Verify(d => d.Dispose(), Times.Never);
            target.Verify(d => d.DisposeAsync(), Times.Once);
        }

        [Test]
        public void ExternallyOwned_ShouldPreventTheValueFromBeingDisposed() 
        {
            var target = new Disposable();
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), target, externallyOwned: true);

            svc.Release();

            Assert.That(target.Disposed, Is.False);

            target.Dispose();
        }

        [Test]
        public void Dependencies_ShouldThrowOnExternallyOwnedService() 
        {
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), value: new Mock<IDummyService>().Object, false);

            Assert.Throws<NotSupportedException>(() => svc.Dependencies.Add(new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), value: new Disposable(), false)));
        }
    }
}
