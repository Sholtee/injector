/********************************************************************************
* ResolverCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;
    using Primitives.Patterns;

    [TestFixture]
    public sealed class ResolverCollectionTests
    {
        [Test]
        public void CreatedResolver_ShouldResolveFromSuperInCaseOfSharedEntry([Values(null, "cica")] string name)
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

            ResolverCollection resolvers = new(new[] { entry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), name).Invoke(mockFactory.Object));
            mockSuperFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void CreatedResolver_ShouldResolveFromCurrentInCaseOfNonSharedEntry1([Values(null, "cica")] string name)
        {
            ScopedServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry, 0))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { entry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), name).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void CreatedResolver_ShouldResolveFromCurrentInCaseOfNonSharedEntry2([Values(null, "cica")] string name)
        {
            TransientServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>());

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(entry))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { entry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), name).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(entry), Times.Once);
        }

        [Test]
        public void CreatedResolver_ShouldBeAssignedToTheProperSlot([Values(null, "cica")] string name)
        {
            ScopedServiceEntry 
                entry1 = new(typeof(IList), name, (_, _) => new List<object>()),
                entry2 = new(typeof(IList<object>), name, (_, _) => new List<object>());

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

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), name).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry1, 0), Times.Once);

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<object>), name).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry2, 1), Times.Once);
        }

        public class MyLiyt<T>: List<T> { }

        [Test]
        public void CreatedResolver_ShouldBeAssignedToTheProperSlot_GenericCase([Values(null, "cica")] string name)
        {
            ScopedServiceEntry entry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface.GetGenericTypeDefinition() == typeof(IList<>)), It.IsAny<int>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { entry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<int>), name).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<string>), name).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<string>)), 1), Times.Once);
        }

        [Test]
        public void CreatedResolver_ShouldBeAssignedToTheProperSlot_NamedCase()
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

            ResolverCollection resolvers = new(new[] { entry1, entry2 });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), 0.ToString()).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 0.ToString()), 0), Times.Once);

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), 1.ToString()).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 1.ToString()), 1), Times.Once);
        }

        [Test]
        public void CreatedResolver_ShouldBeAssignedToTheProperSlot_RegularCase()
        {
            ScopedServiceEntry
                entry1 = new(typeof(IList), null, typeof(MyLiyt<object>)),
                entry2 = new(typeof(IDisposable), null, typeof(Disposable));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), It.IsAny<int>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { entry1, entry2 });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList)), 0), Times.Once);

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IDisposable), null).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IDisposable)), 1), Times.Once);
        }

        [Test]
        public void Get_ShouldSpecializeOnlyWhenNeeeded([Values(null, "cica")] string name)
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

            ResolverCollection resolvers = new(new[] { genericEntry, specializedEntry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<int>), name).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(specializedEntry), Times.Once);

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<object>), name).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<int>))), Times.Once);
        }

        [Test]
        public void Get_ShouldSpecialize1([Values(null, "cica")] string name)
        {
            TransientServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.CreateInstance(It.IsAny<TransientServiceEntry>()))
                .Returns((object) null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { genericEntry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<int>), name).Invoke(mockFactory.Object));
            mockFactory
                .Verify(f => f.CreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<int>))), Times.Once);
        }

        [Test]
        public void Get_ShouldSpecialize2([Values(null, "cica")] string name)
        {
            ScopedServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), 0))
                .Returns((object)null);
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            ResolverCollection resolvers = new(new[] { genericEntry });

            Assert.DoesNotThrow(() => resolvers.Get(typeof(IList<int>), name).Invoke(mockFactory.Object));
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
        public async Task Specialization_ShouldBeThreadSafe()
        {
            BlockingServiceEntry genericEntry = new(typeof(IList<>));

            ResolverCollection resolvers = new(new[] { genericEntry });

            Task<Func<IInstanceFactory, object>[]> t = Task.WhenAll(Enumerable
                .Repeat(0, 20)
                .Select(_ => Task.Run(() => resolvers.Get(typeof(IList<int>), null)))
                .ToArray());

            t.Wait(200);

            genericEntry.Lock.Set();

            Func<IInstanceFactory, object>[] facts = await t;

            Assert.That(facts.Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Get_ShouldCache([Values(null, "cica")] string name)
        {
            ScopedServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>));

            ResolverCollection resolvers = new(new[] { genericEntry });

            Assert.AreSame(resolvers.Get(typeof(IList<int>), name), resolvers.Get(typeof(IList<int>), name));
        }
    }
}
