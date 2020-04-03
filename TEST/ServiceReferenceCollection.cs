﻿/********************************************************************************
* ServiceReferenceCollection.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public class ServiceReferenceCollectionTests
    {
        public interface IDummyService { }

        private ServiceReferenceCollection Collection { get; set; }

        [SetUp]
        public void Setup() => Collection = new ServiceReferenceCollection();

        public void Cleanup() => Collection.Dispose();

        [Test]
        public void Add_ShouldIncrementTheRefCount() 
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>().Object);
            Collection.Add(reference);

            Assert.That(Collection.Count, Is.EqualTo(1));
            Assert.That(Collection.Contains(reference));
            Assert.That(reference.RefCount, Is.EqualTo(2));
        }

        [Test]
        public void Remove_ShouldDecrementTheRefCount() 
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>().Object);
            Collection.Add(reference);

            Assert.That(Collection.Remove(reference));
            Assert.That(Collection, Is.Empty);
            Assert.That(reference.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void Clear_ShouldDecrementTheRefCount()
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>().Object);
            Collection.Add(reference);
            Collection.Clear();

            Assert.That(Collection, Is.Empty);
            Assert.That(reference.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_ShouldDecrementTheRefCount()
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>().Object);
            Collection.Add(reference);
            Collection.Dispose();

            Assert.That(Collection, Is.Empty);
            Assert.That(reference.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposeAsync_ShouldDecrementTheRefCount()
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object), new Mock<IInjector>().Object);
            Collection.Add(reference);
            Collection.DisposeAsync().AsTask().Wait();

            Assert.That(Collection, Is.Empty);
            Assert.That(reference.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void CopyTo_ShouldThrow() 
        {
            var ar = new ServiceReference[0];

            Assert.Throws<NotSupportedException>(() => Collection.CopyTo(ar, 0));
        }
    }
}
