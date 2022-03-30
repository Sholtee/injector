/********************************************************************************
* Dictionary.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Internals;

    [MemoryDiagnoser]
    public class Dictionary
    {
        private const int Keys = 100;

        private Dictionary<int,  object> Dict { get; set; }

        private readonly string[] Names;

        public Dictionary()
        {
            Names = new string[Keys];
            for (int i = 0; i < Keys; i++)
            {
                Names[i] = i.ToString();
            }

            Ifaces = typeof(object)
                .Assembly
                .GetTypes()
                .Take(Keys)
                .ToArray();
        }

        [GlobalSetup(Target = nameof(GetWithKeys))]
        public void SetupWithKeys()
        {
            Dict = new();

            for (int i = 0; i < Keys; i++)
            {
                Dict.Add(ExperimentalScope.HashCombine(typeof(IFormattable), Names[i]), new object());
            }
        }

        [Benchmark(OperationsPerInvoke = Keys)]
        public void GetWithKeys()
        {
            for (int i = 0; i < Keys; i++)
            {
                Dict.TryGetValue(ExperimentalScope.HashCombine(typeof(IFormattable), Names[i]), out object _);
            }
        }

        private readonly Type[] Ifaces;

        [GlobalSetup(Target = nameof(GetWithoutKeys))]
        public void SetupWithoutKeys()
        {
            Dict = new();

            for (int i = 0; i < Keys; i++)
            {
                Dict.Add(ExperimentalScope.HashCombine(Ifaces[i], null), new object());
            }
        }

        [Benchmark(OperationsPerInvoke = Keys)]
        public void GetWithoutKeys()
        {
            for (int i = 0; i < Keys; i++)
            {
                Dict.TryGetValue(ExperimentalScope.HashCombine(Ifaces[i], null), out object _);
            }
        }
    }
}
