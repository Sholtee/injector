using BenchmarkDotNet.Running;

namespace Solti.Utils.DI.Perf
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<InterfaceProxy>();
        }
    }
}
