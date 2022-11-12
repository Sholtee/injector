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

        public static IEnumerable<string> Engines
        {
            get
            {
                yield return ServiceResolverLookupBuilder.DICT;
                yield return ServiceResolverLookupBuilder.BTREE;
            }
        }

        [ParamsSource(nameof(Engines))]
        public string Engine { get; set; }

        private IServiceEntryLookup Lookup { get; set; }

        [GlobalSetup(Target = nameof(Resolve))]
        public void SetupResolve() => Lookup = ServiceResolverLookupBuilder.Build
        (
            Interfaces
                .Take(ServiceCount)
                .Select(t => new TransientServiceEntry(t, null, static (_, _) => null))
                .ToList(),
            new ScopeOptions 
            {
                ServiceResolutionMode = ServiceResolutionMode.JIT,
                Engine = Engine
            }
        );

        private int Index;

        [Benchmark]
        public object Resolve() => Lookup.Get(Interfaces[Index++ % ServiceCount], null);
    }
}
