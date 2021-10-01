﻿/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;

    [MemoryDiagnoser]
    public class ServicePath
    {
        private static IReadOnlyList<AbstractServiceEntry> Entries { get; } = typeof(object)
            .Assembly
            .ExportedTypes
            .Where(t => t.IsInterface && t.IsGenericTypeDefinition)
            .Select(t => new MissingServiceEntry(t, null))
            .Take(10)
            .ToArray();

        private Internals.ServicePath Path { get; set; }

        [Params(1, 5, 10)]
        public int Depth { get; set; }

        [Params(true, false)]
        public bool WithCircularityCheck { get; set; }

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
                    if (WithCircularityCheck)
                        Path.CheckNotCircular();

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
