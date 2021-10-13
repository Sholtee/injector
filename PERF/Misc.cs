/********************************************************************************
* Misc.cs                                                                       *
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

    [Ignore]
    [MemoryDiagnoser]
    public class TypeBench
    {
        [Benchmark]
        public Guid GetGuid() => typeof(TypeBench).GUID;

        [Benchmark]
        public IntPtr GetHandle() => typeof(TypeBench).TypeHandle.Value;
    }
}
