/********************************************************************************
* ArraFactory.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Internals;

    [MemoryDiagnoser]
    public class ArrayFactory
    {
        private class Baseline0
        {
        }

        private class Baseline1
        {
            public object Val = new();
        }

        private class Baseline5
        {
            public object Val1 = new();
            public object Val2 = new();
            public object Val3 = new();
            public object Val4 = new();
            public object Val5 = new();
        }

        private class Baseline20
        {
            public object Val1 = new();
            public object Val2 = new();
            public object Val3 = new();
            public object Val4 = new();
            public object Val5 = new();
            public object Val6 = new();
            public object Val7 = new();
            public object Val8 = new();
            public object Val9 = new();
            public object Val10 = new();
            public object Val11 = new();
            public object Val12 = new();
            public object Val13 = new();
            public object Val14 = new();
            public object Val15 = new();
            public object Val16 = new();
            public object Val17 = new();
            public object Val18 = new();
            public object Val19 = new();
            public object Val20 = new();
        }

        private static IReadOnlyDictionary<int, Action> BaselineActions = new Dictionary<int, Action>()
        {
            { 0, () => new Baseline0() },
            { 1, () => new Baseline1() },
            { 5, () => new Baseline5() },
            { 20, () => new Baseline20() }
        }; 

        [Params(0, 1, 5, 20)]
        public int Count { get; set; }

        public Func<object[]> Factory { get; set; }

        public Action BaselineAction { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Factory = ArrayFactory<object>.Create(Count);
            BaselineAction = BaselineActions[Count];
        }

        [Benchmark]
        public object[] CreateArray() => Factory();

        [Benchmark(Baseline = true)]
        public void Baseline() => BaselineAction();
    }
}