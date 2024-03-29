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

        private static Func<IInjector, Lazy<IInjector>> CreateFactory_Regular()
        {
            ParameterExpression injector = Expression.Parameter(typeof(IInjector));
            return Expression.Lambda<Func<IInjector, Lazy<IInjector>>>(new RegularLazyDependencyResolver().ResolveLazyService(injector, typeof(IInjector), null), injector).Compile();
        }

        private static readonly Func<IInjector, Lazy<IInjector>> RegularFactory = CreateFactory_Regular();

        [Benchmark]
        public object ViaActivator_Regular() => RegularFactory(Injector);

        private static Func<IInjector, ILazy<IInjector>> CreateFactory()
        {
            ParameterExpression injector = Expression.Parameter(typeof(IInjector));
            return Expression.Lambda<Func<IInjector, ILazy<IInjector>>>(new LazyDependencyResolver().ResolveLazyService(injector, typeof(IInjector), null), injector).Compile();
        }

        private static readonly Func<IInjector, ILazy<IInjector>> Factory = CreateFactory();

        [Benchmark]
        public object ViaActivator() => Factory(Injector);
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class DecoratedServiceActivating
    {
        private sealed class DummyInterceptor : IInterfaceInterceptor
        {
            public object Invoke(IInvocationContext context, CallNextDelegate<IInvocationContext, object> callNext) => callNext(context);
        }

        private IServiceActivator Injector { get; set; }

        private AbstractServiceEntry Entry { get; set; }

        [Params(true, false)]
        public bool SimpleDecorate { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            IServiceCollection coll = new DI.ServiceCollection()
                .Service<IService, MyService>(Lifetime.Transient);

            if (SimpleDecorate)
                coll.Decorate((_, _, inst) => inst);
            else
                coll.Decorate<DummyInterceptor>();

            Entry = coll.Last();
            Injector = (IServiceActivator) ScopeFactory.Create(coll).CreateScope();
        }

        [Benchmark]
        public object ViaCreateInstance() => Entry.CreateInstance(Injector, out _);

        [Benchmark]
        public object ViaGetOrCreateInstance() => Injector.GetOrCreateInstance(Entry);
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ServiceActivating
    {
        private IServiceActivator Injector { get; set; }

        private AbstractServiceEntry Entry { get; set; }

        private FactoryDelegate Factory { get; set; }

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
            IServiceCollection coll = new DI.ServiceCollection()
                .Service<IService, MyService>(Lifetime, ServiceOptions.Default with { DisposalMode = DisposalMode });

            Entry = coll.Last();
            Factory = Entry.Factory.Compile();
            Injector = (IServiceActivator) ScopeFactory.Create(coll).CreateScope();
        }

        [Benchmark]
        public object ViaFactory() => Factory(Injector, typeof(IService));

        [Benchmark]
        public object ViaCreateInstance() => Entry.CreateInstance(Injector, out _);

        [Benchmark]
        public object ViaGetOrCreateInstance() => Injector.GetOrCreateInstance(Entry);
    }
}
