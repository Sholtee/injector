/********************************************************************************
* ServiceReferenceCollection.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public class ServiceReferenceCollectionTests
    {
        public interface IDummyService { }

        private ServiceReferenceCollection Collection;

        [SetUp]
        public void Setup() => Collection = new ServiceReferenceCollection();

        [Test]
        public void Add_ShouldIncrementTheRefCount() 
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null));
            Collection.Add(reference);

            Assert.That(Collection.Count, Is.EqualTo(1));
            Assert.That(Collection.Contains(reference));
            Assert.That(reference.RefCount, Is.EqualTo(2));
        }

        [Test]
        public void Remove_ShouldDecrementTheRefCount() 
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null));
            Collection.Add(reference);

            Assert.That(Collection.Remove(reference));
            Assert.That(Collection, Is.Empty);
            Assert.That(reference.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void Clear_ShouldDecrementTheRefCount()
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null));
            Collection.Add(reference);
            Collection.Clear();

            Assert.That(Collection, Is.Empty);
            Assert.That(reference.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_ShouldDecrementTheRefCount()
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null));
            Collection.Add(reference);
            Collection.Dispose();

            Assert.That(Collection, Is.Empty);
            Assert.That(reference.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposeAsync_ShouldDecrementTheRefCount()
        {
            var reference = new ServiceReference(new AbstractServiceEntry(typeof(IDummyService), null));
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
