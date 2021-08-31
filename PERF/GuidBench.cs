/********************************************************************************
* GuidBench.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    //
    // Nem tudtam h a Type.GUID property-t hasznalo resolver-ek miert kibaszott lassuak... Mar tudom...
    //

    [MemoryDiagnoser]
    public class GuidBench
    {
        [Benchmark]
        public Guid GetGuid() => typeof(GuidBench).GUID;

        private static readonly Guid 
            TestGuid1 = Guid.NewGuid(),
            TestGuid2 = Guid.NewGuid();

        [Benchmark]
        public bool CompareGuid() => TestGuid1 == TestGuid2;
    }
}
