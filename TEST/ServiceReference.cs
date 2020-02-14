/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Internals;

    [TestFixture]
    public sealed class ServiceReferenceTests
    {
        private static readonly DummyServiceEntry DummyServiceEntry = new DummyServiceEntry();

        [Test]
        public void Value_CanBeSetOnce() 
        {
            var svc = new ServiceReference(DummyServiceEntry)
            {
                Value = new object()
            };

            Assert.Throws<InvalidOperationException>(() => svc.Value = new object());
        }

        [Test]
        public void Dispose_ShouldDisposeTheValueAndDecrementTheRefCountOfTheDependencies() 
        {         
            var dependency = new ServiceReference(DummyServiceEntry) { Value = new object() }; // refcount == 1

            var target = new Disposable();
            var svc = new ServiceReference(DummyServiceEntry) { Value = target };
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
            var svc = new ServiceReference(DummyServiceEntry) { Value = target };

            svc.SuppressDispose();
            svc.Dispose();

            Assert.That(target.Disposed, Is.False);

            target.Dispose();
        }

        [Test]
        public void SuppressDispose_ShouldThrowIfTheServiceHasDependencies() 
        {
            var svc = new ServiceReference(DummyServiceEntry);
            var depdendency = new ServiceReference(DummyServiceEntry);

            svc.Dependencies.Add(depdendency);

            Assert.Throws<InvalidOperationException>(svc.SuppressDispose);

            svc.Dependencies.Remove(depdendency);

            Assert.DoesNotThrow(svc.SuppressDispose);
        }

        [Test]
        public void Dependencies_ShouldThrowIfDisposeIsSuppressed() 
        {
            var svc = new ServiceReference(DummyServiceEntry);
            svc.SuppressDispose();

            Assert.Throws<InvalidOperationException>(() => _ = svc.Dependencies);
        }
    }
}
