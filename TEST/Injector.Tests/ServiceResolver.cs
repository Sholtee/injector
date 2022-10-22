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
        public static IEnumerable<Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup>> Lookups
        {
            get 
            {
                yield return (entries, mode) => new ServiceResolverLookup_BTree(entries, new ScopeOptions { ServiceResolutionMode = mode });
                yield return (entries, mode) => new ServiceResolverLookup_BuiltBTree(entries, new ScopeOptions { ServiceResolutionMode = mode });
                yield return (entries, mode) => new ArmedServiceResolverLookup(entries, new ScopeOptions { ServiceResolutionMode = mode });
            }
        }

        [Test]
        public void Resolver_ShouldResolveFromSuperFactoryInCaseOfSharedEntry([Values(null, "cica")] string name, [ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            SingletonServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>());

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

            IServiceResolverLookup lookup = lookupFactory(new[] { entry }, resolutionMode);

            ServiceResolver resolver = lookup.Get(typeof(IList), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockSuperFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void Resolver_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry1([Values(null, "cica")] string name, [ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry, 0))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolverLookup lookup = lookupFactory(new[] { entry }, resolutionMode);

            ServiceResolver resolver = lookup.Get(typeof(IList), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void Resolver_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry2([Values(null, "cica")] string name, [ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(entry))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolverLookup lookup = lookupFactory(new[] { entry }, resolutionMode);

            ServiceResolver resolver = lookup.Get(typeof(IList), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(entry), Times.Once);
        }

        public class MyLiyt<T>: List<T> { }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_GenericCase([Values(null, "cica")] string name, [ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry entry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface.GetGenericTypeDefinition() == typeof(IList<>)), It.IsAny<int>()))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolverLookup lookup = lookupFactory(new[] { entry }, resolutionMode);

            ServiceResolver resolver = lookup.Get(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);

            resolver = lookup.Get(typeof(IList<string>), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<string>)), 1), Times.Once);
        }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_NamedCase([ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry
                entry1 = new(typeof(IList), 0.ToString(), typeof(MyLiyt<object>)),
                entry2 = new(typeof(IList), 1.ToString(), typeof(MyLiyt<object>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList)), It.IsAny<int>()))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolverLookup lookup = lookupFactory(new[] { entry1, entry2 }, resolutionMode);

            ServiceResolver resolver = lookup.Get(typeof(IList), 0.ToString());

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 0.ToString()), 0), Times.Once);

            resolver = lookup.Get(typeof(IList), 1.ToString());

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 1.ToString()), 1), Times.Once);
        }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_RegularCase([Values(null, "cica")] string name, [ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry
                entry1 = new(typeof(IList), name, typeof(MyLiyt<object>)),
                entry2 = new(typeof(IDisposable), name, typeof(MyDisposable));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), It.IsAny<int>()))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolverLookup lookup = lookupFactory(new[] { entry1, entry2 }, resolutionMode);

            ServiceResolver resolver = lookup.Get(typeof(IList), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList)), 0), Times.Once);

            resolver = lookup.Get(typeof(IDisposable), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IDisposable)), 1), Times.Once);
        }

        [Test]
        public void Resolver_ShouldSpecializeOnlyIfTheConstructedGenericServiceCannotBeFound([Values(null, "cica")] string name, [ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry 
                genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>)),
                specializedEntry = new(typeof(IList<int>), name, typeof(MyLiyt<int>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(It.IsAny<TransientServiceEntry>()))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolverLookup lookup = lookupFactory(new[] { genericEntry, specializedEntry }, resolutionMode);

            ServiceResolver resolver = lookup.Get(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(specializedEntry), Times.Once);

            resolver = lookup.Get(typeof(IList<object>), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<object>))), Times.Once);
        }

        [Test]
        public void Resolver_ShouldSpecialize1([Values(null, "cica")] string name, [ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(It.IsAny<TransientServiceEntry>()))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolverLookup lookup = lookupFactory(new[] { genericEntry }, resolutionMode);

            ServiceResolver resolver = lookup.Get(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<int>))), Times.Once);
        }

        [Test]
        public void Resolver_ShouldSpecialize2([Values(null, "cica")] string name, [ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), 0))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolverLookup lookup = lookupFactory(new[] { genericEntry }, resolutionMode);

            ServiceResolver resolver = lookup.Get(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => resolver.Resolve(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService([ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(lookupFactory(Array<AbstractServiceEntry>.Empty, resolutionMode).Get(typeof(IList), null));
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService_GenericCase([ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(lookupFactory(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList<object>), null, typeof(MyLiyt<object>)) }, resolutionMode ).Get(typeof(IList<int>), null));
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService_NamedCase([ValueSource(nameof(Lookups))] Func<IEnumerable<AbstractServiceEntry>, ServiceResolutionMode, IServiceResolverLookup> lookupFactory, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(lookupFactory(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList), 0.ToString(), typeof(MyLiyt<object>)) }, resolutionMode).Get(typeof(IList), null));
        }
    }
}
