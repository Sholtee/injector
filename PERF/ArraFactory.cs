/********************************************************************************
* ArraFactory.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Internals;

    [MemoryDiagnoser]
    public class ArraFactory
    {
        [Params(0, 1, 5, 20)]
        public int Count { get; set; }

        public Func<object[]> Factory { get; set; }

        [GlobalSetup]
        public void Setup() => Factory = ArrayFactory<object>.Create(Count);

        [Benchmark]
        public object[] CreateArray() => Factory();
    }
}