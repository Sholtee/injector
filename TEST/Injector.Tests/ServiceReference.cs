﻿/********************************************************************************
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
    using Interfaces;
    using Primitives.Patterns;
    using Properties;

    [TestFixture]
    public sealed class ServiceReferenceTests
    {
        public interface IDummyService { }

        [Test]
        public void Value_CanBeSetOnce() 
        {
            var svc = new ServiceReference(new DummyServiceEntry(typeof(IDummyService), null), new Mock<IInjector>(MockBehavior.Strict).Object)
            {
                Value = new Mock<IDummyService>().Object
            };

            Assert.Throws<InvalidOperationException>(() => svc.Value = new Mock<IDummyService>().Object);
        }

        [Test]
        public void Value_ShouldBeValidated()
        {
            var svc = new ServiceReference(new DummyServiceEntry(typeof(IDummyService), null), new Mock<IInjector>(MockBehavior.Strict).Object);

            Assert.Throws<ArgumentNullException>(() => svc.Value = null);
            Assert.Throws<InvalidCastException>(() => svc.Value = new object(), Resources.INVALID_INSTANCE);
            Assert.DoesNotThrow(() => svc.Value = new Mock<IDummyService>().Object);
        }

        [Test]
        public void Value_MayBeWrapped()
        {
            IDisposable obj = new Disposable();
  
            Mock<IWrapped<object>> mockWrapped = new(MockBehavior.Strict);
            mockWrapped
                .SetupGet(x => x.Value)
                .Returns(obj);

            var svc = new ServiceReference(new DummyServiceEntry(typeof(IDisposable), null), new Mock<IInjector>(MockBehavior.Strict).Object);

            Assert.DoesNotThrow(() => svc.Value = mockWrapped.Object);
        }

        [Test]
        public void WrappedValue_ShouldBeValidated()
        {
            Mock<IWrapped<object>> mockWrapped = new(MockBehavior.Strict);
            mockWrapped
                .SetupGet(x => x.Value)
                .Returns(new object());

            var svc = new ServiceReference(new DummyServiceEntry(typeof(IDisposable), null), new Mock<IInjector>(MockBehavior.Strict).Object);

            Assert.Throws<InvalidCastException>(() => svc.Value = mockWrapped.Object);
        }

        [Test]
        public void Release_ShouldDisposeTheValueAndDecrementTheRefCountOfTheDependencies() 
        {         
            var dependency = new ServiceReference(new DummyServiceEntry(typeof(IDummyService), null), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = new Mock<IDummyService>().Object // refcount == 1
            };

            var target = new Disposable();

            var svc = new ServiceReference(new DummyServiceEntry(typeof(IDisposable), null), new Mock<IInjector>(MockBehavior.Strict).Object);

            svc.AddDependency(dependency);
            svc.Value = target;


            Assert.That(dependency.RefCount, Is.EqualTo(2));

            svc.Release();

            Assert.That(target.Disposed);
            Assert.That(dependency.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseAsync_ShouldDisposeTheValueSynchronouslyAndDecrementTheRefCountOfTheDependencies()
        {
            var dependency = new ServiceReference(new DummyServiceEntry(typeof(IDummyService), null), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = new Mock<IDummyService>().Object 
            };

            var target = new Mock<IDisposable>(MockBehavior.Strict);

            target.Setup(d => d.Dispose());

            var svc = new ServiceReference(new DummyServiceEntry(typeof(IDisposable), null), new Mock<IInjector>(MockBehavior.Strict).Object);

            svc.AddDependency(dependency);
            svc.Value = target.Object;

            Assert.That(dependency.RefCount, Is.EqualTo(2));

            svc.ReleaseAsync().Wait();

            target.Verify(d => d.Dispose(), Times.Once);
            Assert.That(dependency.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseAsync_ShouldDisposeTheValueAsynchronouslyAndDecrementTheRefCountOfTheDependencies()
        {
            var dependency = new ServiceReference(new DummyServiceEntry(typeof(IDummyService), null), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = new Mock<IDummyService>().Object 
            };

            var target = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            target
                .Setup(d => d.DisposeAsync())
                .Returns(default(ValueTask));

            var svc = new ServiceReference(new DummyServiceEntry(typeof(IAsyncDisposable), null), new Mock<IInjector>(MockBehavior.Strict).Object);

            svc.AddDependency(dependency);
            svc.Value = target.Object;

            Assert.That(dependency.RefCount, Is.EqualTo(2));

            svc.ReleaseAsync().Wait();

            target.Verify(d => d.DisposeAsync(), Times.Once);
            Assert.That(dependency.RefCount, Is.EqualTo(1));
        }

        public interface ICompositeDisposable : IDisposable, IAsyncDisposable { }

        [Test]
        public void ReleaseAsync_AsynchronousDisposalShouldHaveHigherPriority()
        {
            var target = new Mock<ICompositeDisposable>(MockBehavior.Strict);
            target
                .Setup(d => d.DisposeAsync())
                .Returns(default(ValueTask));

            var svc = new ServiceReference(new DummyServiceEntry(typeof(ICompositeDisposable), null), new Mock<IInjector>(MockBehavior.Strict).Object) 
            { 
                Value = target.Object
            };

            svc.ReleaseAsync().Wait();

            target.Verify(d => d.Dispose(), Times.Never);
            target.Verify(d => d.DisposeAsync(), Times.Once);
        }

        [Test]
        public void ExternallyOwned_ShouldPreventTheValueFromBeingDisposed() 
        {
            var target = new Disposable();
            var svc = new ServiceReference(new DummyServiceEntry(typeof(IDisposable), null), target, externallyOwned: true);

            svc.Release();

            Assert.That(target.Disposed, Is.False);

            target.Dispose();
        }

        [Test]
        public void AddDependency_ShouldThrowOnExternallyOwnedService() 
        {
            var svc = new ServiceReference(new DummyServiceEntry(typeof(IDummyService), null), value: new Mock<IDummyService>().Object, false);

            Assert.Throws<InvalidOperationException>(() => svc.AddDependency(new ServiceReference(new DummyServiceEntry(typeof(IDisposable), null), value: new Disposable(), false)), Resources.FOREIGN_DEPENDENCY);
        }

        [Test]
        public void AddDependency_ShouldThrowIfTheServiceInstanceIsSet()
        {
            var svc = new ServiceReference(new DummyServiceEntry(typeof(IDummyService), null), injector: new Mock<IInjector>(MockBehavior.Strict).Object);

            Assert.DoesNotThrow(() => svc.AddDependency(new ServiceReference(new DummyServiceEntry(typeof(IDisposable), null), value: new Disposable(), false)));
            svc.Value = new Mock<IDummyService>().Object;

            Assert.Throws<InvalidOperationException>(() => svc.AddDependency(new ServiceReference(new DummyServiceEntry(typeof(IDisposable), null), value: new Disposable(), false)), Resources.FOREIGN_DEPENDENCY);
        }
    }
}
