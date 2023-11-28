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

            Mock<IServiceActivator> mockSuperFactory = new(MockBehavior.Strict);
            mockSuperFactory
                .Setup(f => f.GetOrCreateInstance(entry))
                .Returns(new object());
            mockSuperFactory
                .SetupGet(f => f.Super)
                .Returns((IServiceActivator) null);

            Mock<IServiceActivator> mockActivator = new(MockBehavior.Strict);
            mockActivator
                .SetupGet(f => f.Super)
                .Returns(mockSuperFactory.Object);

            IServiceResolver resolver = new ServiceResolver(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList), name);

            Assert.That(grabed, Is.SameAs(entry));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(0));
        }

        [Test]
        public void Resolver_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry1([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>(), ServiceOptions.Default);

            Mock<IServiceActivator> mockActivator = new(MockBehavior.Strict);
            mockActivator
                .Setup(f => f.GetOrCreateInstance(entry))
                .Returns(new object());
            mockActivator
                .SetupGet(f => f.Super)
                .Returns((IServiceActivator) null);

            IServiceResolver resolver = new ServiceResolver(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList), name);

            Assert.That(grabed, Is.SameAs(entry));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(0));
        }

        [Test]
        public void Resolver_ShouldResolveFromCurrentFactoryInCaseOfNonSharedEntry2([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry entry = new(typeof(IList), name, (_, _) => new List<object>(), ServiceOptions.Default);

            Mock<IServiceActivator> mockActivator = new(MockBehavior.Strict);
            mockActivator
                .Setup(f => f.GetOrCreateInstance(entry))
                .Returns(new object());
            mockActivator
                .SetupGet(f => f.Super)
                .Returns((IServiceActivator) null);

            IServiceResolver resolver = new ServiceResolver(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList), name);

            Assert.That(grabed, Is.SameAs(entry));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(AbstractServiceEntry.Consts.INVALID_SLOT));
        }

        public class MyLiyt<T>: List<T> { }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_GenericCase([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry entry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default);

            Mock<IServiceActivator> mockActivator = new(MockBehavior.Strict);
            mockActivator
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Type.GetGenericTypeDefinition() == typeof(IList<>))))
                .Returns(new object());
            mockActivator
                .SetupGet(f => f.Super)
                .Returns((IServiceActivator) null);

            IServiceResolver resolver = new ServiceResolver(new[] { entry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList<int>), name);

            Assert.That(grabed.Type, Is.EqualTo(typeof(IList<int>)));
            Assert.That(grabed.Key, Is.EqualTo(name));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(0));

            grabed = resolver.Resolve(typeof(IList<string>), name);

            Assert.That(grabed.Type, Is.EqualTo(typeof(IList<string>)));
            Assert.That(grabed.Key, Is.EqualTo(name));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(1));
        }

        // Same interface, differ only in name
        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_NamedCase([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry
                entry1 = new(typeof(IList), 0.ToString(), typeof(MyLiyt<object>), ServiceOptions.Default),
                entry2 = new(typeof(IList), 1.ToString(), typeof(MyLiyt<object>), ServiceOptions.Default);

            Mock<IServiceActivator> mockActivator = new(MockBehavior.Strict);
            mockActivator
                .Setup(f => f.GetOrCreateInstance(It.Is<ScopedServiceEntry>(e => e.Type == typeof(IList))))
                .Returns(new object());
            mockActivator
                .SetupGet(f => f.Super)
                .Returns((IServiceActivator) null);

            IServiceResolver resolver = new ServiceResolver(new[] { entry1, entry2 }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList), 0.ToString());

            Assert.That(grabed.Type, Is.EqualTo(typeof(IList)));
            Assert.That(grabed.Key, Is.EqualTo(0.ToString()));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(0));

            grabed = resolver.Resolve(typeof(IList), 1.ToString());

            Assert.That(grabed.Type, Is.EqualTo(typeof(IList)));
            Assert.That(grabed.Key, Is.EqualTo(1.ToString()));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(1));
        }

        [Test]
        public void Resolver_ShouldBeAssignedToTheProperSlot_RegularCase([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry
                entry1 = new(typeof(IList), name, typeof(MyLiyt<object>), ServiceOptions.Default),
                entry2 = new(typeof(IDisposable), name, typeof(MyDisposable), ServiceOptions.Default);

            Mock<IServiceActivator> mockActivator = new(MockBehavior.Strict);
            mockActivator
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>()))
                .Returns(new object());
            mockActivator
                .SetupGet(f => f.Super)
                .Returns((IServiceActivator) null);

            IServiceResolver resolver = new ServiceResolver(new[] { entry1, entry2 }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList), name);

            Assert.That(grabed.Type, Is.EqualTo(typeof(IList)));
            Assert.That(grabed.Key, Is.EqualTo(name));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(0));

            grabed = resolver.Resolve(typeof(IDisposable), name);

            Assert.That(grabed.Type, Is.EqualTo(typeof(IDisposable)));
            Assert.That(grabed.Key, Is.EqualTo(name));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(1));
        }

        [Test]
        public void Resolver_ShouldSpecializeOnlyIfTheConstructedGenericServiceCannotBeFound([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry 
                genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default),
                specializedEntry = new(typeof(IList<int>), name, typeof(MyLiyt<int>), ServiceOptions.Default);

            Mock<IServiceActivator> mockActivator = new(MockBehavior.Strict);
            mockActivator
                .Setup(f => f.GetOrCreateInstance(It.IsAny<TransientServiceEntry>()))
                .Returns(new object());
            mockActivator
                .SetupGet(f => f.Super)
                .Returns((IServiceActivator) null);

            IServiceResolver resolver = new ServiceResolver(new[] { genericEntry, specializedEntry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList<int>), name);

            Assert.That(grabed, Is.SameAs(specializedEntry));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(AbstractServiceEntry.Consts.INVALID_SLOT));

            grabed = resolver.Resolve(typeof(IList<object>), name);

            Assert.That(grabed.Type, Is.EqualTo(typeof(IList<object>)));
            Assert.That(grabed.Key, Is.EqualTo(name));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(AbstractServiceEntry.Consts.INVALID_SLOT));
        }

        [Test]
        public void Resolver_ShouldSpecialize1([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            TransientServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default);

            Mock<IServiceActivator> mockActivator = new(MockBehavior.Strict);
            mockActivator
                .Setup(f => f.GetOrCreateInstance(It.IsAny<TransientServiceEntry>()))
                .Returns(new object());
            mockActivator
                .SetupGet(f => f.Super)
                .Returns((IServiceActivator) null);

            IServiceResolver resolver = new ServiceResolver(new[] { genericEntry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList<int>), name);

            Assert.That(grabed.Type, Is.EqualTo(typeof(IList<int>)));
            Assert.That(grabed.Key, Is.EqualTo(name));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(AbstractServiceEntry.Consts.INVALID_SLOT));
        }

        [Test]
        public void Resolver_ShouldSpecialize2([Values(null, "cica")] string name, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            ScopedServiceEntry genericEntry = new(typeof(IList<>), name, typeof(MyLiyt<>), ServiceOptions.Default);

            Mock<IServiceActivator> mockActivator = new(MockBehavior.Strict);
            mockActivator
                .Setup(f => f.GetOrCreateInstance(It.IsAny<ScopedServiceEntry>()))
                .Returns(new object());
            mockActivator
                .SetupGet(f => f.Super)
                .Returns((IServiceActivator) null);

            IServiceResolver resolver = new ServiceResolver(new[] { genericEntry }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            AbstractServiceEntry grabed = resolver.Resolve(typeof(IList<int>), name);

            Assert.That(grabed.Type, Is.EqualTo(typeof(IList<int>)));
            Assert.That(grabed.Key, Is.EqualTo(name));
            Assert.That(grabed.AssignedSlot, Is.EqualTo(0));
        }

        [Test]
        public void Resolver_ShouldThrowOnDuplicateService()
        {
            Assert.Throws<InvalidOperationException>
            (
                () => new ServiceResolver
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
                () => new ServiceResolver
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
                () => new ServiceResolver
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

        private sealed class BlockingServiceEntry : ProducibleServiceEntry
        {
            public BlockingServiceEntry(Type @interface) : base(@interface, null, (_, _) => null, ServiceOptions.Default)
            {
            }

            public override AbstractServiceEntry Specialize(params Type[] genericArguments)
            {
                Evt.Wait();
                return new BlockingServiceEntry(Type.MakeGenericType(genericArguments));
            }

            public override ServiceEntryFeatures Features { get; } = ServiceEntryFeatures.SupportsBuild;

            public ManualResetEventSlim Evt = new();
        }

        [Test]
        public void Resolver_ShouldHaveTimeout()
        {
            BlockingServiceEntry entry = new(typeof(IList<>));
            IServiceResolver resolver = new ServiceResolver(new[] { entry }, ScopeOptions.Default with { ResolutionLockTimeout = TimeSpan.Zero });

            //
            // First thread will enter the lock even if the event has not been set
            //

            bool t1Started = false;
            Task<AbstractServiceEntry> t1 = Task<AbstractServiceEntry>.Factory.StartNew(() => { t1Started = true; return resolver.Resolve(typeof(IList<int>), null); });
            SpinWait.SpinUntil(() => t1Started);

            bool t2Started = false;
            Task<AbstractServiceEntry> t2 = Task<AbstractServiceEntry>.Factory.StartNew(() => { t2Started = true; return resolver.Resolve(typeof(IList<int>), null); });
            SpinWait.SpinUntil(() => t2Started);

            entry.Evt.Set();

            Assert.DoesNotThrow(t1.Wait);
            Assert.Throws<TimeoutException>(() => t2.GetAwaiter().GetResult());
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(new ServiceResolver(Array<AbstractServiceEntry>.Empty, new ScopeOptions { ServiceResolutionMode = resolutionMode }).Resolve(typeof(IList), null));
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService_GenericCase([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(new ServiceResolver(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList<object>), null, typeof(MyLiyt<object>), ServiceOptions.Default) }, new ScopeOptions { ServiceResolutionMode = resolutionMode }).Resolve(typeof(IList<int>), null));
        }

        [Test]
        public void Resolver_ShouldReturnNullOnNonRegisteredService_NamedCase([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Assert.IsNull(new ServiceResolver(new AbstractServiceEntry[] { new ScopedServiceEntry(typeof(IList), 0.ToString(), typeof(MyLiyt<object>), ServiceOptions.Default) }, new ScopeOptions { ServiceResolutionMode = resolutionMode }).Resolve(typeof(IList), null));
        }

        [Test]
        public void Resolver_SpecializationShouldBeThreadSafe([Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            IServiceResolver resolver = new ServiceResolver
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
