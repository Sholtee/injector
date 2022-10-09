/********************************************************************************
* ServiceActivator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;

    internal sealed class DummyInjector : IInjector
    {
        public object Tag { get; }

        public ScopeOptions Options { get; }

        public bool Disposed { get; }

        public void Dispose() { }

        public ValueTask DisposeAsync() => default;

        public object Get(Type iface, string name = null) => this;

        public object TryGet(Type iface, string name = null) => this;
    }

    [Ignore]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 1000000)]
    public class ServiceActivator_Lazy
    {
        private static readonly IInjector Injector = new DummyInjector();

        [Benchmark(Baseline = true)]
        public object InstantiateDirectly() => new Lazy<IInjector>(() => (IInjector) Injector.Get(typeof(IInjector)));

        private static Func<IInjector, Lazy<IInjector>> CreateFactory()
        {
            ParameterExpression injector = Expression.Parameter(typeof(IInjector));
            return Expression.Lambda<Func<IInjector, Lazy<IInjector>>>(ServiceActivator.CreateLazy(injector, typeof(IInjector), null), injector).Compile();
        }

        private static readonly Func<IInjector, Lazy<IInjector>> Factory = CreateFactory();

        [Benchmark]
        public object ViaActivator() => Factory(Injector);
    }

    [Ignore]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 1000000)]
    public class ServiceActivator_New
    {
        public class MyClass
        {
            public MyClass(IInjector injector) { }
        }

        private static readonly IInjector Injector = new DummyInjector();

        [Benchmark(Baseline = true)]
        public object InstantiateDirectly() => new MyClass(Injector);

        private static readonly Func<IInjector, Type, object> Factory = ServiceActivator.Get(typeof(MyClass)).Compile();

        [Benchmark]
        public object ViaActivator() => Factory(Injector, typeof(MyClass));
    }
}
