/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Solti.Utils.DI.Perf
{
    using Internals;

    class Program
    {
        static void Main(string[] args)
        {
            //
            // Ez egy ordas nagy hack h meg a tesztek futasa elott behuzza a runtime a Solti.Utils.DI szerelvenyt is
            //

            Trace.WriteLine($"{typeof(Injector)}");

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run
            (
                args
#if DEBUG
                , new DebugInProcessConfig()
#endif
            );
        }
    }
}
