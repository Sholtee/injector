/********************************************************************************
* ProxyInvocation.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;
    using Proxy.Generators;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ProxyInvocation
    {
        public interface IService
        {
            void Foo();
        }

        public class MyService : IService
        {
            public MyService() { }

            public void Foo() { }
        }

        private sealed class DummyInterceptor : IInterfaceInterceptor
        {
            public object Invoke(IInvocationContext context, Next<IInvocationContext, object> callNext) => callNext(context);
        }

        private IService Service { get; set; }

        [GlobalSetup(Target = nameof(UsingInterceptor))]
        public void SetupInterceptor() =>
            Service = ProxyGenerator<IService, AspectAggregator<IService, MyService>>.Activate(Tuple.Create(new MyService(), new IInterfaceInterceptor[] { new DummyInterceptor() }));

        [Benchmark]
        public void UsingInterceptor() => Service.Foo();

        [GlobalSetup(Target = nameof(Baseline))]
        public void SetupBaseline() =>
            Service = new MyService();

        [Benchmark(Baseline = true)]
        public void Baseline() => Service.Foo();
    }
}
