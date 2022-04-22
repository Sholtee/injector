/********************************************************************************
* ResolverCollection.cs                                                         *
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
    [TestFixture]
    public sealed class ResolverCollectionTests
    {
        [Test]
        public void CreatedResolver_ShouldResolveFromSuperInCaseOfSharedEntry()
        {
            SingletonServiceEntry entry = new(typeof(IList), null, (_, _) => new List<object>());

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

            ResolverCollection resolvers = new(new[] { entry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), null).Invoke(mockFactory.Object));
            mockSuperFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void CreatedResolver_ShouldResolveFromCurrentInCaseOfNonSharedEntry1()
        {
            ScopedServiceEntry entry = new(typeof(IList), null, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry, 0))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { entry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void CreatedResolver_ShouldResolveFromCurrentInCaseOfNonSharedEntry2()
        {
            TransientServiceEntry entry = new(typeof(IList), null, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(entry))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { entry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(entry), Times.Once);
        }

        [Test]
        public void CreatedResolver_ShouldBeAssignedToTheProperSlot()
        {
            ScopedServiceEntry 
                entry1 = new(typeof(IList), null, (_, _) => new List<object>()),
                entry2 = new(typeof(IList<object>), null, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry1, 0))
                .Returns((object) null);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry2, 1))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory)null);

            ResolverCollection resolvers = new(new[] { entry1, entry2 });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry1, 0), Times.Once);

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<object>), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry2, 1), Times.Once);
        }

        public class MyLiyt<T>: List<T> { }

        [Test]
        public void CreatedResolver_ShouldBeAssignedToTheProperSlot_GenericCase()
        {
            ScopedServiceEntry entry = new(typeof(IList<>), null, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface.GetGenericTypeDefinition() == typeof(IList<>)), It.IsAny<int>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { entry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<int>), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<string>), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<string>)), 1), Times.Once);
        }
    }
}
