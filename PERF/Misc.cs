/********************************************************************************
* Misc.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Internals;

    //
    // Nem tudtam h a Type.GUID property-t hasznalo resolver-ek miert kibaszott lassuak... Mar tudom...
    //

    [MemoryDiagnoser]
    public class TypeBench
    {
        [Benchmark]
        public Guid GetGuid() => typeof(TypeBench).GUID;

        [Benchmark]
        public IntPtr GetHandle() => typeof(TypeBench).TypeHandle.Value;
    }

    [MemoryDiagnoser]
    public class PoolTools
    {
        [Params(typeof(IList<int>), typeof(IDisposable))]
        public Type Interface { get; set; }

        [Params(null, "cica")]
        public string Name { get; set; }

        [Benchmark]
        public string GetPoolName() => PooledLifetime.GetPoolName(Interface, Name);
    }
}
