/********************************************************************************
* ServiceIdComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using static Internals.ServiceResolverLookup_BTree;

    [Ignore]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 100000000)]
    public class ServiceIdComparer
    {
        private static readonly CompositeKey
            Svc1 = new(typeof(IList<int>), null),
            Svc2 = new(typeof(IList<object>), null),
            Svc3 = new(typeof(IList<int>), "kutya"),
            Svc4 = new(typeof(IList<object>), "cica");

        [Benchmark]
        public int CompareIds() => CompareServiceIds(Svc1, Svc1);

        [Benchmark]
        public int CompareIdsNoMatch() => CompareServiceIds(Svc1, Svc2);

        [Benchmark]
        public int CompareNamedIds() => CompareServiceIds(Svc3, Svc3);

        [Benchmark]
        public int CompareNamedIdsNoMatch() => CompareServiceIds(Svc3, Svc4);
    }
}
