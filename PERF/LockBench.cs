/********************************************************************************
* LockBench.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;

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

    [MemoryDiagnoser]
    public class LockHeldBench
    {
        [Params(true, false)]
        public bool Held { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            if (Held)
                Monitor.Enter(this);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (Held)
                Monitor.Exit(this);
        }

        [Benchmark]
        public void CheckLockHeld() => Monitor.IsEntered(this);
    }
}
