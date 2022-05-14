/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ServiceResolver
    {
        public static Type[] Interfaces { get; } = typeof(object)
            .Assembly
            .GetTypes()
            .Where(t => t.IsInterface && !t.IsGenericTypeDefinition)
            .ToArray();

        [Params(1, 2, 5, 10, 20, 50, 80)]
        public int ServiceCount { get; set; }

        public static IEnumerable<Type> Engines
        {
            get
            {
                yield return typeof(ServiceResolver_BTree);
                yield return typeof(ServiceResolver_Dict);
            }
        }

        [ParamsSource(nameof(Engines))]
        public Type Engine { get; set; }

        private IServiceResolver Resolver { get; set; }

        private sealed class DummyInstanceFactory : Singleton<DummyInstanceFactory>, IInstanceFactory
        {
            private static readonly object Obj = new();

            public IInstanceFactory Super { get; }

            public object CreateInstance(AbstractServiceEntry requested) => Obj;

            public object GetOrCreateInstance(AbstractServiceEntry requested, int slot) => Obj;
        }

        [GlobalSetup]
        public void Setup() => Resolver = (IServiceResolver) Activator.CreateInstance(Engine, new object[] { Interfaces.Take(ServiceCount).Select(t => new TransientServiceEntry(t, null, (_, _) => null)) });

        private int Index;

        [Benchmark]
        public object Resolve() => Resolver.Resolve(Interfaces[Index++ % ServiceCount], null, DummyInstanceFactory.Instance);
    }
}
