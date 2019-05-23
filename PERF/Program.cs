using BenchmarkDotNet.Running;

namespace Solti.Utils.DI.Perf
{
    class Program
    {
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
