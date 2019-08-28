﻿/********************************************************************************
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
    public sealed class ServiceCollectionTests
    {
        [Test]
        public void ServiceCollection_ShouldDisposeOwnedEntriesOnly()
        {
            Disposable 
                owned    = new Disposable(),
                notOwned = new Disposable();

            var collection = new ServiceCollection();

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

            using (var collection = new ServiceCollection(new []{entry.Object}))
            {
                entry.Verify(e => e.CopyTo(It.Is<ServiceCollection>(sc => sc == collection)), Times.Once);
            }
        }

        [Test]
        public void ServiceCollection_ShouldContainUniqueEntries()
        {
            var collection = new ServiceCollection(new []
            {
                new InstanceServiceEntry(typeof(IDisposable), null, false, null)
            });

            Assert.Throws<ServiceAlreadyRegisteredException>(() => collection.Add(new InstanceServiceEntry(typeof(IDisposable), null, false, null)));

            Assert.Throws<ServiceAlreadyRegisteredException>(() => new ServiceCollection(new[]
            {
                new InstanceServiceEntry(typeof(IDisposable), null, false, null),
                new InstanceServiceEntry(typeof(IDisposable), null, false, null)
            }));
        }

        [Test]
        public void ServiceCollection_QueryShouldReturnOnTypeMatch()
        {          
            var collection = new ServiceCollection();
            var entry = new TransientServiceEntry(typeof(IList<>), typeof(List<>), collection);
            collection.Add(entry);

            Assert.That(collection.Query(typeof(IList<>)), Is.EqualTo(entry));
        }

        [Test]
        public void ServiceCollection_QueryShouldReturnIfTheGenerisEntryIsProducible()
        {
            var collection = new ServiceCollection();
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
            var collection = new ServiceCollection(new []
            {
                new TransientServiceEntry(typeof(IList<>), typeof(MyList<>), null)
            });
            Assert.That(collection.Count, Is.EqualTo(1));

            AbstractServiceEntry entry = collection.Query(typeof(IList<int>));
            Assert.That(entry, Is.Not.Null);
            Assert.That(collection.Contains(entry));
            Assert.That(collection.Count, Is.EqualTo(2));
        }

        [Test]
        public void ServiceCollection_QueryShouldSpecializeNotOwnedEtries()
        {
            var parentCollection = new ServiceCollection();
            parentCollection.Add(new SingletonServiceEntry(typeof(IList<>), typeof(MyList<>), parentCollection));

            var childCollection = new ServiceCollection(parentCollection);
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
        public void ServiceCollection_ContainsShouldSearchByReference()
        {
            AbstractServiceEntry 
                entry1 = new AbstractServiceEntry(typeof(IDisposable)),
                entry2 = new AbstractServiceEntry(typeof(IDisposable));

            var collection = new ServiceCollection(new[]{ entry1 });
            
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

            var collection = new ServiceCollection(new[] { entry1 });

            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.That(entry1, Is.EqualTo(entry2));
            Assert.False(collection.Remove(entry2));
            Assert.True(collection.Remove(entry1));
            Assert.That(!collection.Any());
        }
    }
}
