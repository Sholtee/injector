/********************************************************************************
* InterfaceProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    [MemoryDiagnoser]
    public class InterfaceProxy
    {
        private const int OperationsPerInvoke = 50000;
        private const string Param = "";

        private sealed class InterfaceProxyWithoutTarget : InterfaceProxy<IInterface>
        {
            public InterfaceProxyWithoutTarget() : base(null)
            {
            }

            protected override object Invoke(MethodInfo targetMethod, object[] args) => 0;
        }

        private IInterface
            InstanceWithoutProxy,
            InstanceWithProxy,
            InstanceWithProxyWithoutTarget;

        public interface IInterface
        {
            int DoSomething(string param);
        }

        public class Implementation : IInterface
        {
            int IInterface.DoSomething(string param) => 0;
        }

        [GlobalSetup]
        public void Setup()
        {
            InstanceWithoutProxy           = new Implementation();
            InstanceWithProxy              = new InterfaceProxy<IInterface>(InstanceWithoutProxy).Proxy;
            InstanceWithProxyWithoutTarget = new InterfaceProxyWithoutTarget().Proxy;
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
    }
}
