/********************************************************************************
* DuckTyping.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Proxy;
    using static Consts;

    [MemoryDiagnoser]
    public class DuckTyping
    {
        private const string Param = "";

        private Implementation FTarget;
        private IInterface FProxy;

        public sealed class Implementation
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public int DoSomething(string param) => 0;
        }

        [GlobalSetup(Target = nameof(NoProxy))]
        public void SetupTarget() => FTarget = new Implementation();

        [GlobalSetup(Target = nameof(Proxy))]
        public void SetupProxy() => FProxy = (FTarget = new Implementation()).Act().Like<IInterface>();

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoProxy()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FTarget.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Proxy()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FProxy.DoSomething(Param);
            }
        }
    }
}
