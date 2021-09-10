/********************************************************************************
* Dispose.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;
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
                new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance)
                {
                    new TransientServiceEntry(typeof(IDisposable), "owned", typeof(Disposable), null),
                    new SingletonServiceEntry(typeof(IDisposable), "notowned", typeof(Disposable), null)
                },
                resolver,
                CancellationToken.None
            });

            AbstractServiceEntry
                owned,
                notOwned;

            using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry, true }))
            {
                notOwned = child.GetEntry<IDisposable>("notowned");
                owned = child.GetEntry<IDisposable>("owned");
            }

            Assert.IsFalse(notOwned.Disposed);
            Assert.IsTrue(owned.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeNonSharedEntriesOnly([ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance)
                {
                    new TransientServiceEntry(typeof(IDisposable), "owned", typeof(Disposable), null),
                    new SingletonServiceEntry(typeof(IDisposable), "notowned", typeof(Disposable), null)
                },
                resolver,
                CancellationToken.None
            });

            AbstractServiceEntry
                owned,
                notOwned;

            await using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry, true }))
            {
                notOwned = child.GetEntry<IDisposable>("notowned");
                owned = child.GetEntry<IDisposable>("owned");
            }

            Assert.IsFalse(notOwned.Disposed);
            Assert.IsTrue(owned.Disposed);
        }

        [Test]
        public void Dispose_ShouldDisposeChildRegistryAndItsEntries([ValueSource(nameof(RegistryTypes))] Type registryType)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance)
                {
                    new TransientServiceEntry(typeof(IDisposable), null, typeof(Disposable), null)
                },
                null,
                CancellationToken.None
            });

            AbstractServiceEntry entry1, entry2;
            ServiceRegistryBase grandChild1, grandChild2;

            using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry, true }))
            {
                grandChild1 = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { child, true });
                Assert.That(grandChild1.Parent, Is.SameAs(child));

                entry1 = grandChild1.GetEntry<IDisposable>();

                grandChild2 = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { child, true });
                Assert.That(grandChild2.Parent, Is.SameAs(child));

                entry2 = grandChild2.GetEntry<IDisposable>();
            }

            Assert.IsTrue(grandChild1.Disposed);
            Assert.IsTrue(entry1.Disposed);
            Assert.IsTrue(grandChild2.Disposed);
            Assert.IsTrue(entry2.Disposed);
        }

        [Test]
        public void Dispose_ShouldDisposeRegisteredChildRegistriesOnly([ValueSource(nameof(RegistryTypes))] Type registryType)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance)
                {
                    new TransientServiceEntry(typeof(IDisposable), null, typeof(Disposable), null)
                },
                null,
                CancellationToken.None
            });

            ServiceRegistryBase grandChild1, grandChild2;

            using (ServiceRegistryBase child = (ServiceRegistryBase)Activator.CreateInstance(registryType, new object[] { Registry, true }))
            {
                grandChild1 = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { child, true });
                Assert.That(grandChild1.Parent, Is.SameAs(child));

                grandChild2 = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { child, false });
                Assert.That(grandChild2.Parent, Is.SameAs(child));
            }


            Assert.IsTrue(grandChild1.Disposed);
            Assert.IsFalse(grandChild2.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeDescendantRegistries([ValueSource(nameof(RegistryTypes))] Type registryType)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance)
                {
                    new TransientServiceEntry(typeof(IDisposable), null, typeof(Disposable), null)
                },
                null,
                CancellationToken.None
            });

            AbstractServiceEntry entry1, entry2;
            ServiceRegistryBase grandChild1, grandChild2;

            await using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry, true }))
            {
                grandChild1 = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { child, true });
                entry1 = grandChild1.GetEntry<IDisposable>();

                grandChild2 = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { child, true });
                entry2 = grandChild2.GetEntry<IDisposable>();
            }

            Assert.IsTrue(grandChild1.Disposed);
            Assert.IsTrue(entry1.Disposed);
            Assert.IsTrue(grandChild2.Disposed);
            Assert.IsTrue(entry2.Disposed);
        }

        [Test]
        public void Dispose_ShouldDisposeSpecializedEntries([ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance)
                {
                    new TransientServiceEntry(typeof(IList<>), null, typeof(MyList<>), null),
                },
                resolver,
                CancellationToken.None
            });

            AbstractServiceEntry owned;

            using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry, true }))
            {
                owned = child.GetEntry<IList<int>>();
            }

            Assert.IsTrue(owned.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeSpecializedEntries([ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[]
            {
                new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance)
                {
                    new TransientServiceEntry(typeof(IList<>), null, typeof(MyList<>), null),
                },
                resolver,
                CancellationToken.None
            });

            AbstractServiceEntry owned;

            await using (ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry, true }))
            {
                owned = child.GetEntry<IList<int>>();
            }

            Assert.IsTrue(owned.Disposed);
        }
    }
}
