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
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null))
            {
                Value = new Mock<IDummyService>().Object
            };

            Assert.Throws<InvalidOperationException>(() => svc.Value = new Mock<IDummyService>().Object);
        }

        [Test]
        public void SetValue_ShouldValidate() 
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null));

            Assert.Throws<InvalidOperationException>(() => reference.Value = null, Resources.INVALID_INSTANCE);
            Assert.Throws<InvalidOperationException>(() => reference.Value = new object(), Resources.INVALID_INSTANCE);
            Assert.DoesNotThrow(() => reference.Value = new Disposable());
        }

        [Test]
        public void Dispose_ShouldDisposeTheValueAndDecrementTheRefCountOfTheDependencies() 
        {         
            var dependency = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null)) { Value = new Mock<IDummyService>().Object }; // refcount == 1

            var target = new Disposable();
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null)) { Value = target };
            svc.Dependencies.Add(dependency);

            Assert.That(dependency.RefCount, Is.EqualTo(2));

            svc.Dispose();

            Assert.That(target.Disposed);
            Assert.That(dependency.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void SuppressDispose_ShouldPreventTheValueFromBeingDisposed() 
        {
            var target = new Disposable();
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDisposable), null)) { Value = target };

            svc.SuppressDispose();
            svc.Dispose();

            Assert.That(target.Disposed, Is.False);

            target.Dispose();
        }

        [Test]
        public void SuppressDispose_ShouldThrowIfTheServiceHasDependencies() 
        {
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null));
            var depdendency = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null));

            svc.Dependencies.Add(depdendency);

            Assert.Throws<InvalidOperationException>(svc.SuppressDispose);

            svc.Dependencies.Remove(depdendency);

            Assert.DoesNotThrow(svc.SuppressDispose);
        }

        [Test]
        public void Dependencies_ShouldThrowIfDisposeIsSuppressed() 
        {
            var svc = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null));
            svc.SuppressDispose();

            Assert.Throws<InvalidOperationException>(() => _ = svc.Dependencies);
        }
    }
}
