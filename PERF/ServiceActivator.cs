/********************************************************************************
* ServiceActivator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

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
}
