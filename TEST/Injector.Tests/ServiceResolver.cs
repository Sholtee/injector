/********************************************************************************
* ServiceResolver.cs                                                            *
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
    using DI.Tests;
    using Interfaces;
    using Properties;

    [TestFixture]
    internal sealed class ServiceResolverTests
    {
        [Test]
        public void Resolver_ShouldResolveFromSuperFactoryInCaseOfSharedEntry([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
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

            IServiceResolver resolver = ServiceResolver.Create(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockSuperFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void Resolver_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry1([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>(), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry, 0))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = ServiceResolver.Create(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry, 0), Times.Once);
        }

        [Test]
        public void Resolver_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry2([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>(), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(entry, null))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = ServiceResolver.Create(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(entry, null), Times.Once);
        }

        public class MyLiyt<T>: List<T> { }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_GenericCase([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry entry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface.GetGenericTypeDefinition() == typeof(IList<>)), It.IsAny<int>()))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = ServiceResolver.Create(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);

            grabed = resolver.Resolve(typeof(IList<string>), name);

            Assert.DoesNotThrow(() => grabed.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<string>)), 1), Times.Once);
        }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_NamedCase([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
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

            IServiceResolver resolver = ServiceResolver.Create(new[] { entry1, entry2 }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry entry = resolver.Resolve(typeof(IList), 0.ToString());

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 0.ToString()), 0), Times.Once);

            entry = resolver.Resolve(typeof(IList), 1.ToString());

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList) && e.Name == 1.ToString()), 1), Times.Once);
        }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_RegularCase([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
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

            IServiceResolver resolver = ServiceResolver.Create(new[] { entry1, entry2 }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry entry = resolver.Resolve(typeof(IList), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList)), 0), Times.Once);

            entry = resolver.Resolve(typeof(IDisposable), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IDisposable)), 1), Times.Once);
        }

        [Test]
        public void Resolver_ShouldSpecializeOnlyIfTheConstructedGenericServiceCannotBeFound([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
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

            IServiceResolver resolver = ServiceResolver.Create(new[] { genericEntry, specializedEntry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry entry = resolver.Resolve(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(specializedEntry, null), Times.Once);

            entry = resolver.Resolve(typeof(IList<object>), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<object>)), null), Times.Once);
        }

        [Test]
        public void Resolver_ShouldSpecialize1([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<TransientServiceEntry>(), null))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = ServiceResolver.Create(new[] { genericEntry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry entry = resolver.Resolve(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<TransientServiceEntry>(e => e.Interface == typeof(IList<int>)), null), Times.Once);
        }

        [Test]
        public void Resolver_ShouldSpecialize2([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default);

            Mock<IInstanceFactory> mockFactory = new(MockBehavior.Strict);
            mockFactory
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>(), 0))
                .Returns(new object());
            mockFactory
                .SetupGet(f => f.Super)
                .Returns((IInstanceFactory) null);

            IServiceResolver resolver = ServiceResolver.Create(new[] { genericEntry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry entry = resolver.Resolve(typeof(IList<int>), name);

            Assert.DoesNotThrow(() => entry.ResolveInstance(mockFactory.Object));
            mockFactory
                .Verify(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Interface == typeof(IList<int>)), 0), Times.Once);
        }

        [Test]
        public void Resolver_ShouldThrowOnDuplicateService()
        {
            Assert.Throws<InvalidOperationException>
            (
                () => ServiceResolver.Create
                (
                    new AbstractServiceEntry[]
                    {
                        new TransientServiceEntry(typeof(IList), null, (_, _) => null, ServiceOptions.Default),
                        new SingletonServiceEntry(typeof(IList), null, (_, _) => null, ServiceOptions.Default)
                    },
                    ScopeOptions.Default
                ),
                Resources.SERVICE_ALREADY_REGISTERED
            );
        }

        [Test]
        public void Resolver_ShouldThrowOnDuplicateGenericService()
        {
            Assert.Throws<InvalidOperationException>
            (
                () => ServiceResolver.Create
                (
                    new AbstractServiceEntry[]
                    {
                        new TransientServiceEntry(typeof(IList<>), null, (_, _) => null, ServiceOptions.Default),
                        new SingletonServiceEntry(typeof(IList<>), null, (_, _) => null, ServiceOptions.Default)
                    },
                    ScopeOptions.Default
                ),
                Resources.SERVICE_ALREADY_REGISTERED
            );
        }

        [Test]
        public void Resolver_ShouldThrowOnInvalidResolutionMode()
        {
            Assert.Throws<NotSupportedException>
            (
                () => ServiceResolver.Create
                (
                    new AbstractServiceEntry[]
                    {
                        new TransientServiceEntry(typeof(IList<>), null, (_, _) => null, ServiceOptions.Default)
                    },
                    new ScopeOptions 
                    {
                        ServiceResolutionMode = (ServiceResolutionMode) 1986
                    }
                )
            );
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(ServiceResolver.Create(Array<AbstractServiceEntry>.Empty, new ScopeOptions { ServiceResolutionMode = resolutionMode }).Resolve(typeof(IList), null));
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService_GenericCase([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(ServiceResolver.Create(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList<object>), null, typeof(MyLiyt<object>), ServiceOptions.Default) }, new ScopeOptions { ServiceResolutionMode = resolutionMode }).Resolve(typeof(IList<int>), null));
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService_NamedCase([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(ServiceResolver.Create(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList), 0.ToString(), typeof(MyLiyt<object>), ServiceOptions.Default) }, new ScopeOptions { ServiceResolutionMode = resolutionMode }).Resolve(typeof(IList), null));
        }

        [Test]
        public void Resolver_SpecializationShouldBeThreadSafe([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            IServiceResolver resolver = ServiceResolver.Create
            (
                new[]
                {
                    new TransientServiceEntry
                    (
                        typeof(IList<>),
                        null,
                        (_, _) => null,
                        ServiceOptions.Default
                    )
                },
                ScopeOptions.Default with { ServiceResolutionMode = resolutionMode }
            );

            ManualResetEventSlim evt = new();

            Task<AbstractServiceEntry>[] tasks = StartTasks().ToArray();

            Assert.That(Task.WaitAny(tasks, 10, default), Is.EqualTo(-1));

            evt.Set();

            Assert.That(Task.WaitAll(tasks, 10));

            Task<AbstractServiceEntry> prev = null;

            foreach (Task<AbstractServiceEntry> task in tasks)
            {
                Assert.NotNull(task.Result);
                if (prev != null)
                    Assert.AreSame(prev.Result, task.Result);
                prev = task;
            }

            IEnumerable<Task<AbstractServiceEntry>> StartTasks()
            {
                for (int i = 0; i < 5; i++)
                {
                    yield return Task<AbstractServiceEntry>.Factory.StartNew(() =>
                    {
                        evt.Wait();
                        return resolver.Resolve(typeof(IList<int>), null);
                    });
                }
            }
        }
    }
}
