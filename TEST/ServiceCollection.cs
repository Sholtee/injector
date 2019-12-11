﻿/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public class ServiceCollectionTests
    {
        private ServiceCollection Collection;

        [SetUp]
        public void Setup() => Collection = new ServiceCollection();

        [Test]
        public void Add_ShouldIncrementTheRefCount() 
        {
            var reference = new ServiceReference(null, null);
            Collection.Add(reference);

            Assert.That(Collection.Count, Is.EqualTo(1));
            Assert.That(Collection.Contains(reference));
            Assert.That(reference.RefCount, Is.EqualTo(2));
        }

        [Test]
        public void Remove_ShouldDecrementTheRefCount() 
        {
            var reference = new ServiceReference(null, null);
            Collection.Add(reference);

            Assert.That(Collection.Remove(reference));
            Assert.That(Collection, Is.Empty);
            Assert.That(reference.RefCount, Is.EqualTo(1));
        }

        [Test]
        public void Clear_ShouldDecrementTheRefCount()
        {
            var reference = new ServiceReference(null, null);
            Collection.Add(reference);
            Collection.Clear();

            Assert.That(Collection, Is.Empty);
            Assert.That(reference.RefCount, Is.EqualTo(1));
        }
    }
}
