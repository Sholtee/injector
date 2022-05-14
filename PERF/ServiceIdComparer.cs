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
        [Benchmark]
        public int CompareIds() => ServiceResolver_Tree.CompareServiceIds(typeof(IList<int>), null, typeof(IList<int>), null);

        [Benchmark]
        public int CompareIdsNoMatch() => ServiceResolver_Tree.CompareServiceIds(typeof(IList<int>), null, typeof(IList<object>), null);

        [Benchmark]
        public int CompareNamedIds() => ServiceResolver_Tree.CompareServiceIds(typeof(IList<int>), "kutya", typeof(IList<int>), "kutya");

        [Benchmark]
        public int CompareNamedIdsNoMatch() => ServiceResolver_Tree.CompareServiceIds(typeof(IList<int>), "kutya", typeof(IList<int>), "cica");
    }
}
