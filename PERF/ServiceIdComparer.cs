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
    using Internals;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 100000000)]
    public class ServiceIdComparer
    {
        private static readonly long
            svc_1 = (long) typeof(IList<int>).TypeHandle.Value,
            svc_2 = (long) typeof(IList<object>).TypeHandle.Value;

        [Benchmark]
        public int CompareIds() => ServiceResolver_BTree.CompareServiceIds(svc_1, null, svc_1, null);

        [Benchmark]
        public int CompareIdsNoMatch() => ServiceResolver_BTree.CompareServiceIds(svc_1, null, svc_2, null);

        [Benchmark]
        public int CompareNamedIds() => ServiceResolver_BTree.CompareServiceIds(svc_1, "kutya", svc_1, "kutya");

        [Benchmark]
        public int CompareNamedIdsNoMatch() => ServiceResolver_BTree.CompareServiceIds(svc_1, "kutya", svc_1, "cica");
    }
}
