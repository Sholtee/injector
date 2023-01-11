/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using DI.Tests;
    using Interfaces;

    [TestFixture]
    internal sealed class ServiceResolverTests
    {
        [Test]
        public void Resolver_ShouldResolveFromSuperFactoryInCaseOfSharedEntry([Values(null, "cica")] string name, [Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            SingletonServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>(), ServiceOptions.Default);

            Mock<IInstanceFactory> mockSuperFactory = new(MockBehavior.Strict);
            mockSuperFactory
                .Setup(f => f.GetOrCreateInstance(entry, 0))
                .Returns(new object());
            mockSuperFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns(mockSuperFactory.Object);

            IServiceEntryLookup lookup = ServiceEntryLookupBuilder.Build(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine });

            AbstractServiceEntry grabed = lookup.Get(typeof(IList), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockSuperFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void Resolver_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry1([Values(null, "cica")] string name, [Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>(), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry, 0))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceEntryLookup lookup = ServiceEntryLookupBuilder.Build(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine });

            AbstractServiceEntry grabed = lookup.Get(typeof(IList), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void Resolver_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry2([Values(null, "cica")] string name, [Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>(), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry, null))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceEntryLookup lookup = ServiceEntryLookupBuilder.Build(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine });

            AbstractServiceEntry grabed = lookup.Get(typeof(IList), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry, null), Times.Once);
        }

        public class MyLiyt<T>: List<T> { }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_GenericCase([Values(null, "cica")] string name, [Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry entry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface.GetGenericTypeDefinition() == typeof(IList<>)), It.IsAny<int>()))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceEntryLookup lookup = ServiceEntryLookupBuilder.Build(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine });

            AbstractServiceEntry grabed = lookup.Get(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);

            grabed = lookup.Get(typeof(IList<string>), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<string>)), 1), Times.Once);
        }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_NamedCase([Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry
                entry1 = new(typeof(IList), 0.ToString(), typeof(MyLiyt<object>), ServiceOptions.Default),
                entry2 = new(typeof(IList), 1.ToString(), typeof(MyLiyt<object>), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList)), It.IsAny<int>()))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceEntryLookup lookup = ServiceEntryLookupBuilder.Build(new[] { entry1, entry2 }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine });

            AbstractServiceEntry entry = lookup.Get(typeof(IList), 0.ToString());

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 0.ToString()), 0), Times.Once);

            entry = lookup.Get(typeof(IList), 1.ToString());

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 1.ToString()), 1), Times.Once);
        }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_RegularCase([Values(null, "cica")] string name, [Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry
                entry1 = new(typeof(IList), name, typeof(MyLiyt<object>), ServiceOptions.Default),
                entry2 = new(typeof(IDisposable), name, typeof(MyDisposable), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), It.IsAny<int>()))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceEntryLookup lookup = ServiceEntryLookupBuilder.Build(new[] { entry1, entry2 }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine });

            AbstractServiceEntry entry = lookup.Get(typeof(IList), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList)), 0), Times.Once);

            entry = lookup.Get(typeof(IDisposable), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IDisposable)), 1), Times.Once);
        }

        [Test]
        public void Resolver_ShouldSpecializeOnlyIfTheConstructedGenericServiceCannotBeFound([Values(null, "cica")] string name, [Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry 
                genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default),
                specializedEntry = new(typeof(IList<int>), name, typeof(MyLiyt<int>), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<TransientServiceEntry>(), null))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceEntryLookup lookup = ServiceEntryLookupBuilder.Build(new[] { genericEntry, specializedEntry }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine });

            AbstractServiceEntry entry = lookup.Get(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(specializedEntry, null), Times.Once);

            entry = lookup.Get(typeof(IList<object>), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<object>)), null), Times.Once);
        }

        [Test]
        public void Resolver_ShouldSpecialize1([Values(null, "cica")] string name, [Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<TransientServiceEntry>(), null))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceEntryLookup lookup = ServiceEntryLookupBuilder.Build(new[] { genericEntry }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine });

            AbstractServiceEntry entry = lookup.Get(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<int>)), null), Times.Once);
        }

        [Test]
        public void Resolver_ShouldSpecialize2([Values(null, "cica")] string name, [Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), 0))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceEntryLookup lookup = ServiceEntryLookupBuilder.Build(new[] { genericEntry }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine });

            AbstractServiceEntry entry = lookup.Get(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService([Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(ServiceEntryLookupBuilder.Build(Array<AbstractServiceEntry>.Empty, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine }).Get(typeof(IList), null));
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService_GenericCase([Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(ServiceEntryLookupBuilder.Build(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList<object>), null, typeof(MyLiyt<object>), ServiceOptions.Default) }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine }).Get(typeof(IList<int>), null));
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService_NamedCase([Values(ServiceEntryLookupBuilder.DICT, ServiceEntryLookupBuilder.BTREE)] string engine, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(ServiceEntryLookupBuilder.Build(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList), 0.ToString(), typeof(MyLiyt<object>), ServiceOptions.Default) }, new ScopeOptions { ServiceResolutionMode = resolutionMode, Engine = engine }).Get(typeof(IList), null));
        }
    }
}
