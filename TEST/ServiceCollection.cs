/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

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
            collection.Add(new InstanceServiceEntry(typeof(IList<>) /*tok mind1*/, notOwned, releaseOnDispose: true, owner: null));

            Assert.That(collection.Count, Is.EqualTo(2));

            collection.Dispose();
            
            Assert.That(collection.Count, Is.EqualTo(0));
            Assert.That(owned.Disposed);
            Assert.That(notOwned.Disposed, Is.False);
        }

        [Test]
        public void ServiceCollection_ShouldCopyTheInheritedEntries()
        {
            Mock<ServiceEntry> entry = new Mock<ServiceEntry>(MockBehavior.Strict, typeof(IDisposable) /*iface*/, Lifetime.Transient, new ServiceCollection());              
            entry.Setup(e => e.CopyTo(It.IsAny<ServiceCollection>())).Returns<ServiceCollection>(sc => null);

            using (var collection = new ServiceCollection(new []{entry.Object}))
            {
                entry.Verify(e => e.CopyTo(It.Is<ServiceCollection>(sc => sc == collection)), Times.Once);
            }
        }
    }
}
