/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;

    [MemoryDiagnoser]
    public class ServicePath
    {
        private static IReadOnlyList<AbstractServiceEntry> Entries { get; } = typeof(object)
            .Assembly
            .ExportedTypes
            .Where(t => t.IsInterface && !t.IsGenericTypeDefinition)
            .Select(t => new DummyServiceEntry(t, null))
            .Take(10)
            .ToArray();

        private Internals.ServicePath Path { get; set; }

        [Params(1, 5, 10)]
        public int Depth { get; set; }

        [GlobalSetup]
        public void SetupExtend()
        {
            Path = new Internals.ServicePath();
        }

        [Benchmark]
        public void Extend()
        {
            Extend(0);

            void Extend(int i)
            {
                Path.Push(Entries[i]);
                try
                {
                    if (i < Depth - 1)
                        Extend(++i);
                }
                finally 
                {
                    Path.Pop();
                }
            }
        }
    }
}
