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

    [Ignore]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 1000000)]
    public class ServiceActivator_Lazy
    {
        [Benchmark(Baseline = true)]
        public object InstantiateDirectly()
        {
            IInjector injector = null;
            return new Lazy<ICloneable>(() => (ICloneable) injector.Get(typeof(ICloneable)));
        }

        private static Func<IInjector, Lazy<ICloneable>> CreateFactory()
        {
            ParameterExpression injector = Expression.Parameter(typeof(IInjector));
            return Expression.Lambda<Func<IInjector, Lazy<ICloneable>>>(ServiceActivator.CreateLazy(injector, typeof(ICloneable), null), injector).Compile();
        }

        private static readonly Func<IInjector, Lazy<ICloneable>> Factory = CreateFactory();

        [Benchmark]
        public object ViaActivator() => Factory(null);
    }

    [Ignore]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 1000000)]
    public class ServiceActivator_New
    {
        public class DummyInjector : IInjector
        {
            public object Lifetime { get; }

            public ScopeOptions Options { get; }

            public bool Disposed { get; }

            public void Dispose() {}

            public ValueTask DisposeAsync() => default;

            public object Get(Type iface, string name = null) => this;

            public object TryGet(Type iface, string name = null) => this;
        }

        public class MyClass
        {
            public MyClass(IInjector injector) { }
        }

        private static readonly IInjector Injector = new DummyInjector();

        [Benchmark(Baseline = true)]
        public object InstantiateDirectly() => new MyClass(Injector);

        private static readonly Func<IInjector, Type, object> Factory = ServiceActivator.Get(typeof(MyClass));

        [Benchmark]
        public object ViaActivator() => Factory(Injector, typeof(MyClass));
    }
}
