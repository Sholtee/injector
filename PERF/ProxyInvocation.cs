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
    using Proxy;
    using Proxy.Generators;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ProxyInvocation
    {
        public interface IService
        {
            object Foo();
        }

        public class MyService : IService
        {
            public MyService() { }

            public object Foo() => null;
        }

        private sealed class DummyInterceptor : IInterfaceInterceptor
        {
            public object Invoke(IInvocationContext context, NextDelegate<IInvocationContext, object> callNext) => callNext(context);
        }

        private IService Service { get; set; }

        [GlobalSetup(Target = nameof(UsingInterceptor))]
        public void SetupInterceptor() =>
            Service = ProxyGenerator<IService, InterceptorAggregator<IService, MyService>>.Activate(Tuple.Create(new MyService(), new IInterfaceInterceptor[] { new DummyInterceptor() }));

        [Benchmark]
        public void UsingInterceptor() => Service.Foo();

        [GlobalSetup(Target = nameof(Baseline))]
        public void SetupBaseline() => Service = new MyService();

        [Benchmark(Baseline = true)]
        public void Baseline() => Service.Foo();

        private IInvocationContext Context { get; set; }

        private void SetupContext()
        {
            Context = IInvocationContextFactory.Create
            (
                new InvocationContext
                (
                    Array.Empty<object>(),
                    new MethodContext
                    (
                        (instance, args) => ((IService) instance).Foo(),
                        null
                    )
                ),
                new InterceptorAggregator<IService, IService>
                (
                    new MyService(),
                    new DummyInterceptor()
                )
            );
        }

        [GlobalSetup(Target = nameof(InvokeSingleInterceptor))]
        public void SetupInvokeSingleInterceptor() => SetupContext();

        [Benchmark]
        public object InvokeSingleInterceptor() => Context.InvokeInterceptor();

        [GlobalSetup(Target = nameof(JumpToNextInterceptor))]
        public void SetupJumpToNextInterceptor() => SetupContext();

        [Benchmark]
        public object JumpToNextInterceptor() => Context.Next;
    }
}
