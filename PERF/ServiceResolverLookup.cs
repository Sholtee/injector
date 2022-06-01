/********************************************************************************
* ServiceResolverLookup.cs                                                      *
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

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ServiceResolverLookup
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
                yield return typeof(ServiceResolverLookup_BTree);
                yield return typeof(ServiceResolverLookup_BuiltBTree);
                yield return typeof(ServiceResolverLookup_Dict);
            }
        }

        [ParamsSource(nameof(Engines))]
        public Type Engine { get; set; }

        private IServiceResolverLookup Lookup { get; set; }

        [GlobalSetup(Target = nameof(Resolve))]
        public void SetupResolve() => Lookup = (IServiceResolverLookup) Activator.CreateInstance
        (
            Engine,
            new object[]
            {
                Interfaces
                    .Take(ServiceCount)
                    .Select(t => new TransientServiceEntry(t, null, (_, _) => null)),
                new ScopeOptions 
                {
                    ServiceResolutionMode = ServiceResolutionMode.JIT
                }
            }
        );

        private int Index;

        [Benchmark]
        public object Resolve() => Lookup.Get(Interfaces[Index++ % ServiceCount], null);
    }
}
