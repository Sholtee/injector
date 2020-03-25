/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Internals;

    using static Consts;

    [MemoryDiagnoser]
    public class Composite
    {
        private interface IMyComposite : IComposite<IMyComposite> { }

        private sealed class MyComposite : Composite<IMyComposite>, IMyComposite
        {
            public MyComposite() : base(null) { }
        }

        private IMyComposite FRoot;

        [GlobalSetup]
        public void Setup() => FRoot = new MyComposite();

        [GlobalCleanup]
        public void Cleanup() => FRoot.Dispose();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Add_Remove() 
        {
            using (var child = new MyComposite())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    FRoot.Children.Add(child);
                    FRoot.Children.Remove(child);
                }
            }
        }
    }
}
