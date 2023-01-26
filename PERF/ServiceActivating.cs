﻿/********************************************************************************
* ServiceActivating.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;

    public interface IService { }

    public class MyService : IService
    {
        public MyService() { }
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class LazyActivating
    {
        private IInjector Injector { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Injector = ScopeFactory.Create
            (
                coll => coll.Service<IService, MyService>(Lifetime.Transient)
            ).CreateScope();
        }

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

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ServiceActivating
    {
        private IServiceFactory Injector { get; set; }

        private AbstractServiceEntry Entry { get; set; }

        public static IEnumerable<Lifetime> Lifetimes
        {
            get
            {
                yield return Lifetime.Singleton;
                yield return Lifetime.Scoped;
                yield return Lifetime.Transient;
            }
        }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime Lifetime { get; set; }

        [Params(ServiceDisposalMode.Suppress, ServiceDisposalMode.Force)]
        public ServiceDisposalMode DisposalMode { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            IServiceCollection coll = DI.ServiceCollection.Create()
                .Service<IService, MyService>(Lifetime, ServiceOptions.Default with { DisposalMode = DisposalMode });

            Entry = coll.Last();
            Injector = (IServiceFactory) ScopeFactory.Create(coll).CreateScope();
        }

        [Benchmark]
        public object ViaFactory() => Entry.CreateInstance(Injector, out _);

        [Benchmark]
        public object ViaResolver() => Injector.GetOrCreateInstance(Entry);
    }
}
