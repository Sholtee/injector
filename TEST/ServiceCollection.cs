/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public class ServiceCollectionTests
    {
        internal virtual ServiceCollection CreateCollection(params AbstractServiceEntry[] entries) => new ServiceCollection(entries);

        [Test]
        public void ServiceCollection_ShouldDisposeOwnedEntriesOnly()
        {
            Disposable 
                owned    = new Disposable(),
                notOwned = new Disposable();

            ServiceCollection collection = CreateCollection();

            collection.Add(new InstanceServiceEntry(typeof(IDisposable), owned, releaseOnDispose: true, owner: collection));
            collection.Add(new InstanceServiceEntry(typeof(IServiceContainer) /*tok mind1*/, notOwned, releaseOnDispose: true, owner: null));

            Assert.That(collection.Count, Is.EqualTo(2));

            collection.Dispose();
            
            Assert.That(collection.Count, Is.EqualTo(0));
            Assert.That(owned.Disposed);
            Assert.That(notOwned.Disposed, Is.False);
        }

        [Test]
        public void ServiceCollection_ShouldCopyTheInheritedEntries()
        {
            //
            // MockBehavior ne legyen megadva h mikor a GC felszabaditja a mock entitast
            // akkor az ne hasaljon el azert mert a Dispose(bool)-ra (ami egyebkent vedett
            // tag) nem volt hivva a Setup().
            //

            Mock<AbstractServiceEntry> entry = new Mock<AbstractServiceEntry>(typeof(IDisposable) /*iface*/, Lifetime.Transient, new ServiceCollection());              
            entry.Setup(e => e.CopyTo(It.IsAny<ServiceCollection>())).Returns<ServiceCollection>(sc => null);

            using (ServiceCollection collection = CreateCollection(entry.Object))
            {
                entry.Verify(e => e.CopyTo(It.Is<ServiceCollection>(sc => sc == collection)), Times.Once);
            }
        }

        [Test]
        public void ServiceCollection_ShouldContainUniqueEntries()
        {
            ServiceCollection collection = CreateCollection
            (
                new InstanceServiceEntry(typeof(IDisposable), null, false, null)
            );

            Assert.Throws<ServiceAlreadyRegisteredException>(() => collection.Add(new InstanceServiceEntry(typeof(IDisposable), null, false, null)));

            Assert.Throws<ServiceAlreadyRegisteredException>(() => CreateCollection
            (
                new InstanceServiceEntry(typeof(IDisposable), null, false, null),
                new InstanceServiceEntry(typeof(IDisposable), null, false, null)
            ));
        }

        [Test]
        public void ServiceCollection_QueryShouldReturnOnTypeMatch()
        {          
            ServiceCollection collection = CreateCollection();
            var entry = new TransientServiceEntry(typeof(IList<>), typeof(List<>), collection);
            collection.Add(entry);

            Assert.That(collection.Query(typeof(IList<>)), Is.EqualTo(entry));
        }

        [Test]
        public void ServiceCollection_QueryShouldReturnIfTheGenerisEntryIsProducible()
        {
            ServiceCollection collection = CreateCollection();
            var entry = new TransientServiceEntry(typeof(IList<>), (injector, type) => null, collection);
            collection.Add(entry);

            Assert.That(collection.Query(typeof(IList<int>)), Is.EqualTo(entry));
        }

        private class MyList<T> : List<T> // azert kell leszarmazni h pontosan egy konstruktorunk legyen
        {          
            public MyList()
            {               
            }
        }

        [Test]
        public void ServiceCollection_QueryShouldSpecialize()
        {
            ServiceCollection collection = CreateCollection
            (
                new TransientServiceEntry(typeof(IList<>), typeof(MyList<>), null)
            );
            Assert.That(collection.Count, Is.EqualTo(1));

            AbstractServiceEntry entry = collection.Query(typeof(IList<int>));
            Assert.That(entry, Is.Not.Null);
            Assert.That(collection.Contains(entry));
            Assert.That(collection.Count, Is.EqualTo(2));
        }

        [Test]
        public void ServiceCollection_QueryShouldSpecializeNotOwnedEtries()
        {
            ServiceCollection parentCollection = CreateCollection();
            parentCollection.Add(new SingletonServiceEntry(typeof(IList<>), typeof(MyList<>), parentCollection));

            ServiceCollection childCollection = CreateCollection(parentCollection.ToArray());
            Assert.That(childCollection.Count, Is.EqualTo(1));

            AbstractServiceEntry entry = childCollection.Query(typeof(IList<int>));
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Owner, Is.EqualTo(parentCollection));

            Assert.That(childCollection.Contains(entry));
            Assert.That(childCollection.Count, Is.EqualTo(2));
            Assert.That(parentCollection.Contains(entry));
            Assert.That(parentCollection.Count, Is.EqualTo(2));
        }

        [Test]
        public void ServiceCollection_GetShouldReturnExistingEntriesOnly()
        {
            ServiceCollection collection = CreateCollection
            (
                new SingletonServiceEntry(typeof(IList<>), typeof(List<>), null) 
            );
            
            Assert.IsNull(collection.Get(typeof(IList<int>)));
            Assert.AreEqual(new SingletonServiceEntry(typeof(IList<>), typeof(List<>), null), collection.Get(typeof(IList<>)));
        }

        [Test]
        public void ServiceCollection_ContainsShouldSearchByReference()
        {
            AbstractServiceEntry 
                entry1 = new AbstractServiceEntry(typeof(IDisposable)),
                entry2 = new AbstractServiceEntry(typeof(IDisposable));

            ServiceCollection collection = CreateCollection(entry1);
            
            Assert.That(entry1, Is.EqualTo(entry2));
            Assert.True(collection.Contains(entry1));
            Assert.False(collection.Contains(entry2));
        }

        [Test]
        public void ServiceCollection_RemoveShouldRemoveByReference()
        {
            AbstractServiceEntry
                entry1 = new AbstractServiceEntry(typeof(IDisposable)),
                entry2 = new AbstractServiceEntry(typeof(IDisposable));

            ServiceCollection collection = CreateCollection(entry1);

            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.That(entry1, Is.EqualTo(entry2));
            Assert.False(collection.Remove(entry2));
            Assert.True(collection.Remove(entry1));
            Assert.That(collection, Is.Empty);
        }
    }

    [TestFixture]
    public class ConcurrentServiceCollectionTests: ServiceCollectionTests
    {
        internal override ServiceCollection CreateCollection(params AbstractServiceEntry[] entries) => new ConcurrentServiceCollection(entries);

        [Test]
        public void ServiceCollection_EnumeratorShouldBeIndependent()
        {
            var entry = new AbstractServiceEntry(typeof(IDisposable));

            ServiceCollection collection = CreateCollection(entry);

            using (IEnumerator<AbstractServiceEntry> enumerator = collection.GetEnumerator())
            {
                Assert.That(collection.Remove(entry));
                Assert.That(collection, Is.Empty);
                Assert.That(enumerator.MoveNext);
                Assert.AreSame(enumerator.Current, entry);
            }
        }
    }
}
