/********************************************************************************
* ServiceEntryResolver.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ServiceEntryResolver
    {
        public static Type[] Interfaces { get; } = typeof(object)
            .Assembly
            .GetTypes()
            .Where(t => t.IsInterface && !t.IsGenericTypeDefinition)
            .ToArray();

        [Params(1, 2, 5, 10, 20, 50, 80)]
        public int ServiceCount { get; set; }

        private Internals.ServiceEntryResolver Resolver { get; set; }

        [GlobalSetup(Target = nameof(Resolve))]
        public void SetupResolve() => Resolver = ServiceEntryResolverBuilder.Build
        (
            Interfaces
                .Take(ServiceCount)
                .Select(t => new TransientServiceEntry(t, null, static (_, _) => null, ServiceOptions.Default))
                .ToList(),
            new ScopeOptions 
            {
                ServiceResolutionMode = ServiceResolutionMode.JIT
            }
        );

        private int Index;

        [Benchmark]
        public object Resolve() => Resolver.Resolve(Interfaces[Index++ % ServiceCount], null);
    }
}
