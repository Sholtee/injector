/********************************************************************************
* ResolverCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;

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

        [Test]
        public void Get_ShouldSpecializeOnlyWhenNeeeded()
        {
            TransientServiceEntry 
                genericEntry = new(typeof(IList<>), null, typeof(MyLiyt<>)),
                specializedEntry = new(typeof(IList<int>), null, typeof(MyLiyt<int>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(It.IsAny<TransientServiceEntry>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { genericEntry, specializedEntry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<int>), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(specializedEntry), Times.Once);

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<object>), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<int>))), Times.Once);
        }

        [Test]
        public void Get_ShouldSpecialize1()
        {
            TransientServiceEntry genericEntry = new(typeof(IList<>), null, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(It.IsAny<TransientServiceEntry>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { genericEntry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<int>), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<int>))), Times.Once);
        }

        [Test]
        public void Get_ShouldSpecialize2()
        {
            ScopedServiceEntry genericEntry = new(typeof(IList<>), null, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), 0))
                .Returns((object)null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { genericEntry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<int>), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);
        }

        private class BlockingServiceEntry : AbstractServiceEntry
        {
            public readonly ManualResetEventSlim Lock = new();

            public BlockingServiceEntry(Type iface) : base(iface, null) { }

            public override object CreateInstance(IInjector scope, out object lifetime)
            {
                throw new NotImplementedException();
            }

            public override AbstractServiceEntry Specialize(params Type[] genericArguments)
            {
                Lock.Wait();
                return new BlockingServiceEntry(Interface.MakeGenericType(genericArguments));
            }
        }

        [Test]
        public void Specialization_ShouldBeThreadSafe()
        {
            BlockingServiceEntry genericEntry = new(typeof(IList<>));

            ResolverCollection resolvers = new(new[] { genericEntry });

            Func<IInstanceFactory, object> factory1 = null, factory2 = null;

            Task t1 = Task.Run(() => factory1 = resolvers.Get(typeof(IList<int>), null));
            Assert.False(t1.Wait(100));

            Task t2 = Task.Run(() => factory2 = resolvers.Get(typeof(IList<int>), null));
            Assert.False(t2.Wait(100));

            genericEntry.Lock.Set();

            Task.WaitAll(t1, t2);

            Assert.AreSame(factory1, factory2);
        }

        [Test]
        public void Specialization_ShouldBeCached()
        {
            ScopedServiceEntry genericEntry = new(typeof(IList<>), null, typeof(MyLiyt<>));

            ResolverCollection resolvers = new(new[] { genericEntry });

            Assert.AreSame(resolvers.Get(typeof(IList<int>), null), resolvers.Get(typeof(IList<int>), null));
        }
    }
}
