/********************************************************************************
* Dispose.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    internal partial class ServiceRegistryTests
    {
        [Test]
        public void Dispose_ShouldDisposeNonSharedEntriesOnly([ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = new ServiceRegistry
            (
                new AbstractServiceEntry[] 
                {
                    new TransientServiceEntry(typeof(IDisposable), "owned", typeof(Disposable), null, int.MaxValue),
                    new SingletonServiceEntry(typeof(IDisposable), "notowned", typeof(Disposable), null)
                },
                resolver
            );

            AbstractServiceEntry
                owned,
                notOwned;

            using (ServiceRegistry child = new(Registry))
            {
                notOwned = child.GetEntry(typeof(IDisposable), "notowned");
                owned = child.GetEntry(typeof(IDisposable), "owned");
            }

            Assert.IsFalse(notOwned.Disposed);
            Assert.IsTrue(owned.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeNonSharedEntriesOnly([ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = new ServiceRegistry
            (
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IDisposable), "owned", typeof(Disposable), null, int.MaxValue),
                    new SingletonServiceEntry(typeof(IDisposable), "notowned", typeof(Disposable), null)
                },
                resolver
            );

            AbstractServiceEntry
                owned,
                notOwned;

            await using (ServiceRegistry child = new(Registry))
            {
                notOwned = child.GetEntry(typeof(IDisposable), "notowned");
                owned = child.GetEntry(typeof(IDisposable), "owned");
            }

            Assert.IsFalse(notOwned.Disposed);
            Assert.IsTrue(owned.Disposed);
        }

        [Test]
        public void Dispose_ShouldDisposeChildRegistryAndItsEntries()
        {
            Registry = new ServiceRegistry
            (
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IDisposable), null, typeof(Disposable), null, int.MaxValue)
                }
            );

            AbstractServiceEntry entry;
            ServiceRegistry grandChild;

            using (ServiceRegistry child = new(Registry))
            {
                grandChild = new ServiceRegistry(child);
                entry = grandChild.GetEntry(typeof(IDisposable), null);
            }

            Assert.IsTrue(grandChild.Disposed);
            Assert.IsTrue(entry.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeDescendantRegistries()
        {
            Registry = new ServiceRegistry
            (
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IDisposable), null, typeof(Disposable), null, int.MaxValue)
                }
            );

            AbstractServiceEntry entry;
            ServiceRegistry grandChild;

            await using (ServiceRegistry child = new(Registry))
            {
                grandChild = new ServiceRegistry(child);
                entry = grandChild.GetEntry(typeof(IDisposable), null);
            }

            Assert.IsTrue(grandChild.Disposed);
            Assert.IsTrue(entry.Disposed);
        }

        [Test]
        public void Dispose_ShouldDisposeSpecializedEntries([ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = new ServiceRegistry
            (
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IList<>), null, typeof(MyList<>), null, int.MaxValue),
                },
                resolver
            );

            AbstractServiceEntry owned;

            using (ServiceRegistry child = new(Registry))
            {
                owned = child.GetEntry(typeof(IList<int>), null);
            }

            Assert.IsTrue(owned.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeSpecializedEntries([ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = new ServiceRegistry
            (
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IList<>), null, typeof(MyList<>), null, int.MaxValue),
                },
                resolver
            );

            AbstractServiceEntry owned;

            await using (ServiceRegistry child = new(Registry))
            {
                owned = child.GetEntry(typeof(IList<int>), null);
            }

            Assert.IsTrue(owned.Disposed);
        }
    }
}
