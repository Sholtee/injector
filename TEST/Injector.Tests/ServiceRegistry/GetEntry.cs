/********************************************************************************
* GetEntry.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;
    using Primitives.Patterns;

    internal partial class ServiceRegistryTests
    {
        [Test]
        public void GetEntry_ShouldReturnOnTypeMatch([Values(null, "cica")] string name, [ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            TransientServiceEntry entry = new(typeof(IDisposable), name, typeof(Disposable), null);
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver, CancellationToken.None } );

            Assert.That(Registry.GetEntry<IDisposable>(name), Is.Not.Null);
        }

        [Test]
        public void GetEntry_ShouldCache_RegularCase([Values(null, "cica")] string name, [ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            TransientServiceEntry entry = new(typeof(IDisposable), name, typeof(Disposable), null);
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver, CancellationToken.None });

            Assert.AreSame(Registry.GetEntry<IDisposable>(name), Registry.GetEntry(typeof(IDisposable), name));
        }

        [Test]
        public void GetEntry_ShouldBeThreadSafe_RegularCase([Values(null, "cica")] string name, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            TransientServiceEntry entry = new(typeof(IDisposable), name, typeof(Disposable), null);
            Registry = new ConcurrentServiceRegistry(new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver);

            AbstractServiceEntry[] results = null;

            Assert.DoesNotThrowAsync(async () => results = await Task.WhenAll(Enumerable
                .Repeat(0, 40)
                .Select(_ => Task.Run(() => Registry.GetEntry<IDisposable>(name)))));

            results = results.Distinct().ToArray();

            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results.Single(), Is.Not.Null);
        }

        [Test]
        public void GetEntry_ShouldSpecialize([Values(null, "cica")] string name, [ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            TransientServiceEntry entry = new(typeof(IList<>), name, typeof(MyList<>), null);
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver, CancellationToken.None });

            AbstractServiceEntry specialized = Registry.GetEntry<IList<int>>(name);
            Assert.That(specialized, Is.Not.Null);
            Assert.That(specialized, Is.InstanceOf<TransientServiceEntry>());
            Assert.That(specialized.Interface, Is.EqualTo(typeof(IList<int>)));
        }

        [Test]
        public void GetEntry_ShouldCache_GenericCase([Values(null, "cica")] string name, [ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            TransientServiceEntry entry = new(typeof(IList<>), name, typeof(MyList<>), null);
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver, CancellationToken.None });

            Assert.AreSame(Registry.GetEntry(typeof(IList<int>), name), Registry.GetEntry(typeof(IList<int>), name));
        }

        [Test]
        public void GetEntry_ShouldBeThreadSafe_GenericCase([Values(null, "cica")] string name, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            TransientServiceEntry entry = new(typeof(IList<>), name, typeof(MyList<>), null);
            Registry = new ConcurrentServiceRegistry(new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver);

            AbstractServiceEntry[] results = null;

            Assert.DoesNotThrowAsync(async () => results = await Task.WhenAll(Enumerable
                .Repeat(0, 40)
                .Select(_ => Task.Run(() => Registry.GetEntry<IList<int>>(name)))));

            results = results.Distinct().ToArray();

            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results.Single(), Is.Not.Null);
        }

        [Test]
        public void GetEntry_ShouldResolveNonSharedEntriesFromTheCurrentRegistry_RegularCase([Values(null, "cica")] string name, [Values(typeof(ICustomFormatter), typeof(IList<int>))] Type serviceType, [ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            TransientServiceEntry entry = new(serviceType, name, (_, _) => null, null);
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver, CancellationToken.None });

            ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry });

            entry = (TransientServiceEntry) Registry.GetEntry(serviceType, name);

            Assert.That(entry, Is.Not.Null);
            Assert.AreNotSame(entry, child.GetEntry(serviceType, name));
        }

        [Test]
        public void GetEntry_ShouldResolveNonSharedEntriesFromTheCurrentRegistry_GenericCase([Values(null, "cica")] string name, [ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            TransientServiceEntry entry = new(typeof(IList<>), name, typeof(MyList<>), null);
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver, CancellationToken.None });

            ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry });

            Assert.AreNotSame(Registry.GetEntry<IList<int>>(name), child.GetEntry<IList<int>>(name));
        }

        [Test]
        public void GetEntry_ShouldResolveSharedEntriesFromTheParentRegistry_RegularCase([Values(null, "cica")] string name, [Values(typeof(ICustomFormatter), typeof(IList<int>))] Type serviceType, [ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            SingletonServiceEntry entry = new(serviceType, name, (_, _) => null, null);
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver, CancellationToken.None });

            ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry });

            entry = (SingletonServiceEntry) Registry.GetEntry(serviceType, name);

            Assert.That(entry, Is.Not.Null);
            Assert.AreSame(entry, child.GetEntry(serviceType, name));
        }

        [Test]
        public void GetEntry_ShouldResolveSharedEntriesFromTheParentRegistry_GenericCase([Values(null, "cica")] string name, [ValueSource(nameof(RegistryTypes))] Type registryType, [ValueSource(nameof(ResolverBuilders))] ResolverBuilder resolver)
        {
            SingletonServiceEntry entry = new(typeof(IList<>), name, typeof(MyList<>), null);
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { entry }, resolver, CancellationToken.None });

            ServiceRegistryBase child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry });
            Assert.AreSame(Registry.GetEntry<IList<int>>(name), child.GetEntry<IList<int>>(name));
        }
    }

    //
    // 1) Ne generikus alatt legyen nested-kent (mert akkor valojaban "MyList<TParent, T>" a definicio).
    // 2) Azert kell leszarmazni h pontosan egy konstruktorunk legyen
    //

    public class MyList<T> : List<T>
    {
    }
}
