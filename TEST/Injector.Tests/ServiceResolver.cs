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
    public sealed class ServiceResolverTests
    {
        public static IEnumerable<Func<IEnumerable<AbstractServiceEntry>, IServiceResolver>> ResolverFactories
        {
            get 
            {
                yield return entries => new ServiceResolver(entries);
            }
        }

        [Test]
        public void Resolve_ShouldResolveFromSuperFactoryInCaseOfSharedEntry([Values(null, "cica")] string name, [ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            SingletonServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockSuperFactory = new(MockBehavior.Strict);
            mockSuperFactory
                .Setup(f => f.GetOrCreateInstance(entry, 0))
                .Returns((object) null);
            mockSuperFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns(mockSuperFactory.Object);

            IServiceResolver resolver = resolverFactory(new[] { entry });

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList), name, mockFactory.Object));
            mockSuperFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void Resolve_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry1([Values(null, "cica")] string name, [ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            ScopedServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry, 0))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = resolverFactory(new[] { entry });

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void Resolve_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry2([Values(null, "cica")] string name, [ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            TransientServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(entry))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = resolverFactory(new[] { entry });

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(entry), Times.Once);
        }

        public class MyLiyt<T>: List<T> { }

        [Test]
        public void Resolve_ShouldAssignTheProperSlot_GenericCase([Values(null, "cica")] string name, [ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            ScopedServiceEntry entry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface.GetGenericTypeDefinition() == typeof(IList<>)), It.IsAny<int>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = resolverFactory(new[] { entry });

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList<int>), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList<string>), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<string>)), 1), Times.Once);
        }

        [Test]
        public void Resolve_ShouldAssignTheProperSlot_NamedCase([ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            ScopedServiceEntry
                entry1 = new(typeof(IList), 0.ToString(), typeof(MyLiyt<object>)),
                entry2 = new(typeof(IList), 1.ToString(), typeof(MyLiyt<object>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList)), It.IsAny<int>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = resolverFactory(new[] { entry1, entry2 });

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList), 0.ToString(), mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 0.ToString()), 0), Times.Once);

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList), 1.ToString(), mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 1.ToString()), 1), Times.Once);
        }

        [Test]
        public void Resolve_ShouldAssignTheProperSlot_RegularCase([Values(null, "cica")] string name, [ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            ScopedServiceEntry
                entry1 = new(typeof(IList), name, typeof(MyLiyt<object>)),
                entry2 = new(typeof(IDisposable), name, typeof(MyDisposable));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), It.IsAny<int>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = resolverFactory(new[] { entry1, entry2 });

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList)), 0), Times.Once);

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IDisposable), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IDisposable)), 1), Times.Once);
        }

        [Test]
        public void Resolve_ShouldSpecializeOnlyWhenNeeeded([Values(null, "cica")] string name, [ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            TransientServiceEntry 
                genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>)),
                specializedEntry = new(typeof(IList<int>), name, typeof(MyLiyt<int>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(It.IsAny<TransientServiceEntry>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = resolverFactory(new[] { genericEntry, specializedEntry });

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList<int>), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(specializedEntry), Times.Once);

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList<object>), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<int>))), Times.Once);
        }

        [Test]
        public void Resolve_ShouldSpecialize1([Values(null, "cica")] string name, [ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            TransientServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(It.IsAny<TransientServiceEntry>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = resolverFactory(new[] { genericEntry });

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList<int>), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<int>))), Times.Once);
        }

        [Test]
        public void Resolve_ShouldSpecialize2([Values(null, "cica")] string name, [ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            ScopedServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), 0))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = resolverFactory(new[] { genericEntry });

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IList<int>), name, mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);
        }

        [Test]
        public void Resolve_ShouldReturnNullOnNonRegisteredService([ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            Assert.IsNull(resolverFactory(Array<AbstractServiceEntry>.Empty).Resolve(typeof(IList), null, null));
        }

        [Test]
        public void Resolve_ShouldReturnNullOnNonRegisteredService_GenericCase([ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            Assert.IsNull(resolverFactory(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList<object>), null, typeof(MyLiyt<object>)) } ).Resolve(typeof(IList<int>), null, null));
        }

        [Test]
        public void Resolve_ShouldReturnNullOnNonRegisteredService_NamedCase([ValueSource(nameof(ResolverFactories))] Func<IEnumerable<AbstractServiceEntry>, IServiceResolver> resolverFactory)
        {
            Assert.IsNull(resolverFactory(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList), 0.ToString(), typeof(MyLiyt<object>)) }).Resolve(typeof(IList), null, null));
        }
    }
}
