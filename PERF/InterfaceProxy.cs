/********************************************************************************
* InterfaceProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Proxy;

    [MemoryDiagnoser]
    public class InterfaceProxy
    {
        private const int OperationsPerInvoke = 50000;
        private const string Param = "";

        private IInterface
            FInstanceWithoutProxy,
            FProxyWithTarget,
            FProxyWithoutTarget,
            FDispatchProxyWithoutTarget;

        public class Implementation : IInterface
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            int IInterface.DoSomething(string param) => 0;
        }

        [GlobalSetup(Target = nameof(NoProxy))]
        public void SetupNoProxy() => FInstanceWithoutProxy = new Implementation();

        [GlobalSetup(Target = nameof(Proxy))]
        public void SetupProxy() => FProxyWithTarget = ProxyFactory.Create<IInterface, InterfaceProxyWithTarget>(new Implementation());

        [GlobalSetup(Target = nameof(ProxyWithoutTarget))]
        public void SetupProxyWithoutTarget() => FProxyWithoutTarget = ProxyFactory.Create<IInterface, InterfaceProxyWithoutTarget>(new Type[0]);

        [GlobalSetup(Target = nameof(DispatchProxyWithoutTarget))]
        public void SetupDispatchPropxyWithoutTarget() => FDispatchProxyWithoutTarget = DispatchProxy.Create<IInterface, DispatchProxyWithoutTarget>();

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoProxy()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FInstanceWithoutProxy.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Proxy()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FProxyWithTarget.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void ProxyWithoutTarget()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FProxyWithoutTarget.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void DispatchProxyWithoutTarget()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FDispatchProxyWithoutTarget.DoSomething(Param);
            }
        }
    }
}
