/********************************************************************************
* Activator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Internals;

    [MemoryDiagnoser]
    public class Activator
    {
        private const int OperationsPerInvoke = 50000;

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoActivator()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                object o = new List<int>(0);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void BuiltInActivator()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                object o = System.Activator.CreateInstance(typeof(List<int>), 0);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void CreateInstance()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                object o = typeof(List<int>).CreateInstance(new []{typeof(int)}, 0);
            }
        }
    }
}
