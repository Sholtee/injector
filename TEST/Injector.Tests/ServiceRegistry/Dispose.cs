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
        public void Dispose_ShouldDisposeNonSharedEntriesOnly([ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IDisposable), "owned", typeof(Disposable), null, int.MaxValue),
                    new SingletonServiceEntry(typeof(IDisposable), "notowned", typeof(Disposable), null)
                },
                resolver,
                int.MaxValue
            });

            AbstractServiceEntry
                owned,
                notOwned;

            using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry }))
            {
                notOwned = child.GetEntry(typeof(IDisposable), "notowned");
                owned = child.GetEntry(typeof(IDisposable), "owned");
            }

            Assert.IsFalse(notOwned.Disposed);
            Assert.IsTrue(owned.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeNonSharedEntriesOnly([ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IDisposable), "owned", typeof(Disposable), null, int.MaxValue),
                    new SingletonServiceEntry(typeof(IDisposable), "notowned", typeof(Disposable), null)
                },
                resolver,
                int.MaxValue
            });

            AbstractServiceEntry
                owned,
                notOwned;

            await using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry }))
            {
                notOwned = child.GetEntry(typeof(IDisposable), "notowned");
                owned = child.GetEntry(typeof(IDisposable), "owned");
            }

            Assert.IsFalse(notOwned.Disposed);
            Assert.IsTrue(owned.Disposed);
        }

        [Test]
        public void Dispose_ShouldDisposeChildRegistryAndItsEntries([ValueSource(nameof(RegistryTypes))] Type registryType)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IDisposable), null, typeof(Disposable), null, int.MaxValue)
                },
                null,
                int.MaxValue
            });

            AbstractServiceEntry entry;
            ServiceRegistryBase grandChild;

            using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry }))
            {
                grandChild = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { child });
                entry = grandChild.GetEntry(typeof(IDisposable), null);
            }

            Assert.IsTrue(grandChild.Disposed);
            Assert.IsTrue(entry.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeDescendantRegistries([ValueSource(nameof(RegistryTypes))] Type registryType)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IDisposable), null, typeof(Disposable), null, int.MaxValue)
                },
                null,
                int.MaxValue
            });

            AbstractServiceEntry entry;
            ServiceRegistryBase grandChild;

            await using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry }))
            {
                grandChild = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { child }); ;
                entry = grandChild.GetEntry(typeof(IDisposable), null);
            }

            Assert.IsTrue(grandChild.Disposed);
            Assert.IsTrue(entry.Disposed);
        }

        [Test]
        public void Dispose_ShouldDisposeSpecializedEntries([ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IList<>), null, typeof(MyList<>), null, int.MaxValue),
                },
                resolver,
                int.MaxValue
            });

            AbstractServiceEntry owned;

            using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry }))
            {
                owned = child.GetEntry(typeof(IList<int>), null);
            }

            Assert.IsTrue(owned.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeSpecializedEntries([ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new AbstractServiceEntry[]
                {
                    new TransientServiceEntry(typeof(IList<>), null, typeof(MyList<>), null, int.MaxValue),
                },
                resolver,
                int.MaxValue
            });

            AbstractServiceEntry owned;

            await using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry }))
            {
                owned = child.GetEntry(typeof(IList<int>), null);
            }

            Assert.IsTrue(owned.Disposed);
        }
    }
}
