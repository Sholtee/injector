/********************************************************************************
* LockBench.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    [MemoryDiagnoser]
    public class LockBench
    {
        [Params(1, 2, 5, 10)]
        public int Depth { get; set; }

        [Benchmark]
        public void Lock() => LockCore(Depth);

        private void LockCore(int depth)
        {
            lock (this)
                if (depth > 1)
                    LockCore(depth - 1);
        }
    }
}
