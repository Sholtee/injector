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

    [Ignore]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 100000000)]
    public class ServiceIdComparer
    {
        private static readonly long
            svc_1 = (long) typeof(IList<int>).TypeHandle.Value,
            svc_2 = (long) typeof(IList<object>).TypeHandle.Value;

        private static readonly string
            name_1 = new("kutya"),
            name_2 = new("kutya"), // different reference
            name_3 = "kutya_2";

        [Benchmark]
        public int CompareIds() => ServiceResolver_BTree.CompareServiceIds(svc_1, null, svc_1, null);

        [Benchmark]
        public int CompareIdsNoMatch() => ServiceResolver_BTree.CompareServiceIds(svc_1, null, svc_2, null);

        [Benchmark]
        public int CompareNamedIds() => ServiceResolver_BTree.CompareServiceIds(svc_1, name_1, svc_1, name_2);

        [Benchmark]
        public int CompareNamedIdsNoMatch() => ServiceResolver_BTree.CompareServiceIds(svc_1, name_1, svc_1, name_3);
    }
}
