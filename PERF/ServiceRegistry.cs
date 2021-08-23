/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;
    using Primitives.Threading;

    [MemoryDiagnoser]
    public class ServiceRegistry
    {
        [Params(1, 10, 20, 100, 1000)]
        public int ServiceCount { get; set; }

        private ServiceRegistryImpl Registry { get; set; }

        private int I { get; set; }

        [GlobalSetup(Target = nameof(Get))]
        public void SetupGet()
        {
            Registry = new ServiceRegistryImpl(Enumerable.Repeat(0, ServiceCount).Select((_, i) => new TransientServiceEntry(typeof(IList), i.ToString(), (i, t) => null!, new DI.ServiceContainer(), int.MaxValue)));
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry Get() => Registry!.Get(typeof(IList), (++I % ServiceCount).ToString())!;

        [GlobalSetup(Target = nameof(Specialize))]
        public void SetupSpecialize()
        {
            Registry = new ServiceRegistryImpl(Enumerable.Repeat(0, ServiceCount).Select((_, i) => new TransientServiceEntry(typeof(IList<>), i.ToString(), (i, t) => null!, new DI.ServiceContainer(), int.MaxValue)));
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry Specialize() => Registry!.Get(typeof(IList<object>), (++I % ServiceCount).ToString())!;
    }

    internal class ConcurrentList<T> : ICollection<T>
    {
        private readonly ConcurrentLinkedList<T> FUnderlyingList = new();

        public int Count => FUnderlyingList.Count;

        public bool IsReadOnly { get; }

        public void Add(T item) => FUnderlyingList.Add(new LinkedListNode<T> { Value = item });

        public IEnumerator<T> GetEnumerator() => FUnderlyingList.Select(node => node.Value).GetEnumerator()!;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Clear() => throw new NotImplementedException();

        public bool Contains(T item) => throw new NotImplementedException();

        public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();

        public bool Remove(T item) => throw new NotImplementedException();
    }

    internal class ServiceRegistryImpl : Disposable
    {
        private delegate AbstractServiceEntry Resolver(ServiceRegistryImpl self, Type iface, string name);

        private readonly ConcurrentList<AbstractServiceEntry> FUsedEntries = new(); // Add() lehet hivva parhuzamosan

        private readonly Resolver FResolverChain;

        private readonly AbstractServiceEntry[] FLocalEntries;

        private readonly ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>[] FLocalDicts;

        private readonly object[] FLocalLocks;

        private sealed class BuilderContext
        {
            public int GenericEntryCount { get; set; }
            public int RegularEntryCount { get; set; }
        }

        private static T[] Repeat<T>(Func<T> factory, int count)
        {
            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = factory();
            }

            return result;
        }

        private static Resolver Build(AbstractServiceEntry entry, BuilderContext context, Resolver next) // elvileg csak a root-ban lehet hivva
        {
            Resolver resolver;

            if (entry.Interface.IsGenericTypeDefinition)
            {
                ISupportsSpecialization supportsSpecialization = (ISupportsSpecialization) entry;

                int dictIndex = context.GenericEntryCount++;

                resolver = (self, iface, name) =>
                {
                    if (entry.Interface.GUID == iface.GUID && entry.Name == name)
                    {
                        ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>> specializedEntries = self.FLocalDicts[dictIndex];

                        return specializedEntries
                            .GetOrAdd(iface, _ => new Lazy<AbstractServiceEntry>(Specialize, LazyThreadSafetyMode.ExecutionAndPublication))
                            .Value;

                        AbstractServiceEntry Specialize()
                        {
                            AbstractServiceEntry specializedEntry = supportsSpecialization.Specialize(iface.GenericTypeArguments);

                            self.FUsedEntries.Add(specializedEntry);
                            return specializedEntry;
                        }
                    }
                    return next(self, iface, name);
                };
            }
            else
            {
                int entryIndex = context.RegularEntryCount++;

                resolver = (self, iface, name) =>
                {
                    if (entry.Interface == iface && entry.Name == name)
                    {
                        ref AbstractServiceEntry usedEntry = ref self.FLocalEntries[entryIndex];

                        if (usedEntry is null)
                        {
                            lock (self.FLocalLocks[entryIndex]) // usedEntry csak egyszer legyen masolva
                            {
                                if (usedEntry is null)
                                {
                                    usedEntry = entry.Copy();
                                    self.FUsedEntries.Add(usedEntry);
                                }
                            }
                        }

                        return usedEntry;
                    }
                    return next(self, iface, name);
                };
            }

            if (entry.IsShared)
            {
                Resolver baseResolver = resolver;

                resolver = (self, iface, name) => self.Root is not null
                    ? self.Root.Get(iface, name)
                    : baseResolver(self, iface, name);
            }

            return resolver;
        }

        public ServiceRegistryImpl Root { get; }

        public ServiceRegistryImpl(IEnumerable<AbstractServiceEntry> entries)
        {
            FResolverChain = (_, _, _) => null;
            BuilderContext context = new();

            foreach (AbstractServiceEntry entry in entries)
            {
                FResolverChain = Build(entry, context, FResolverChain);
            }

            FLocalEntries = new AbstractServiceEntry[context.RegularEntryCount];
            FLocalLocks = Repeat(() => new object(), context.RegularEntryCount);
            FLocalDicts = Repeat(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), context.GenericEntryCount);
        }

        public ServiceRegistryImpl(ServiceRegistryImpl root)
        {
            Root = root;
            FResolverChain = root.FResolverChain;
            FLocalEntries = new AbstractServiceEntry[root.FLocalEntries.Length];
            FLocalLocks = Repeat(() => new object(), root.FLocalLocks.Length);
            FLocalDicts = Repeat(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), root.FLocalDicts.Length);
        }

        public AbstractServiceEntry Get(Type iface, string name) => FResolverChain.Invoke(this, iface, name);

    }
}
