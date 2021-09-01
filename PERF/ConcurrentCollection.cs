/********************************************************************************
* ConcurrentCollection.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 30000)]
    public class ConcurrentCollection
    {
        public ICollection<object> Collection { get; set; }

        [GlobalSetup]
        public void Setup() => Collection = new Internals.ConcurrentCollection<object>();

        [Benchmark]
        public void Add() => Collection.Add(null);
    }
}
