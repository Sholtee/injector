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
            InstanceWithoutProxy,
            InstanceWithProxy,
            InstanceWithProxyWithoutTarget,
            DispatchPropxyWithoutTarget;

        public class Implementation : IInterface
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            int IInterface.DoSomething(string param) => 0;
        }

        [GlobalSetup]
        public void Setup()
        {
            InstanceWithoutProxy           = new Implementation();
            InstanceWithProxy              = ProxyFactory.Create<IInterface, InterfaceProxyWithTarget>(new Implementation());
            InstanceWithProxyWithoutTarget = ProxyFactory.Create<IInterface, InterfaceProxyWithoutTarget>(new Type[0]);
            DispatchPropxyWithoutTarget    = DispatchProxy.Create<IInterface, DispatchProxyWithoutTarget>();
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoProxy()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                InstanceWithoutProxy.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Proxy()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                InstanceWithProxy.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void ProxyWithoutTarget()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                InstanceWithProxyWithoutTarget.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void DispatchProxyWithoutTarget()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                DispatchPropxyWithoutTarget.DoSomething(Param);
            }
        }
    }
}
