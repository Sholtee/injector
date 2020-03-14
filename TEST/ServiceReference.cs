/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Properties;
    using Internals;

    [TestFixture]
    public sealed class ServiceReferenceTests
    {
        public interface IDummyService { }

        [Test]
        public void Value_CanBeSetOnce() 
        {
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null), new Mock<IInjector>().Object)
            {
                Value = new Mock<IDummyService>().Object
            };

            Assert.Throws<InvalidOperationException>(() => svc.Value = new Mock<IDummyService>().Object);
        }

        [Test]
        public void SetValue_ShouldValidate() 
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null), new Mock<IInjector>().Object);

            Assert.Throws<ArgumentNullException>(() => reference.Value = null);
            Assert.Throws<InvalidOperationException>(() => reference.Value = new object(), Resources.INVALID_INSTANCE);
            Assert.DoesNotThrow(() => reference.Value = new Disposable());
        }

        [Test]
        public void Dispose_ShouldDisposeTheValueAndDecrementTheRefCountOfTheDependencies() 
        {         
            var dependency = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null), new Mock<IInjector>().Object) { Value = new Mock<IDummyService>().Object }; // refcount == 1

            var target = new Disposable();
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null), new Mock<IInjector>().Object) { Value = target };
            svc.Dependencies.Add(dependency);

            Assert.That(dependency.RefCount, Is.EqualTo(2));

            svc.Dispose();

            Assert.That(target.Disposed);
            Assert.That(dependency.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void ExternallyOwned_ShouldPreventTheValueFromBeingDisposed() 
        {
            var target = new Disposable();
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null), target, externallyOwned: true);

            svc.Dispose();

            Assert.That(target.Disposed, Is.False);

            target.Dispose();
        }

        [Test]
        public void Dependencies_ShouldThrowOnExternallyOwnedService() 
        {
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null), value: new Mock<IDummyService>().Object, false);

            Assert.Throws<NotSupportedException>(() => svc.Dependencies.Add(new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null), value: new Disposable(), false)));
        }
    }
}
