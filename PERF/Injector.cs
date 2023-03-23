/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;

    public class InjectorTestsBase
    {
        public const int INVOCATION_COUNT = 2000000;

        public static IEnumerable<LifetimeBase> Lifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
                yield return Lifetime.Singleton;
                yield return Lifetime.Pooled.Using(PoolConfig.Default with { Capacity = 4 });
            }
        }

        [Params(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)]
        public ServiceResolutionMode ResolutionMode { get; set; }

        #region Services
        public interface IDependency
        {
        }

        public class Dependency : IDependency
        {
        }

        public interface IDependant
        {
            IDependency Dependency { get; }
        }

        public class Dependant : IDependant
        {
            public Dependant(IDependency dependency) => Dependency = dependency;
            public IDependency Dependency { get; }
        }

        public interface IDependant<T>
        {
            IDependency Dependency { get; }
        }

        public class Dependant<T> : IDependant<T>
        {
            public Dependant(IDependency dependency) => Dependency = dependency;
            public IDependency Dependency { get; }
        }

        public interface IDependantLazy
        {
            Lazy<IDependency> LazyDependency { get; }
        }

        public class DependantLazy : IDependantLazy
        {
            public DependantLazy(Lazy<IDependency> dependency) => LazyDependency = dependency;
            public Lazy<IDependency> LazyDependency { get; }
        }
        #endregion

        protected IScopeFactory Root { get; private set; }

        protected void Setup(Action<IServiceCollection> setupContainer) => Root = ScopeFactory.Create(setupContainer, new ScopeOptions { ServiceResolutionMode = ResolutionMode });

        [GlobalCleanup]
        public virtual void Cleanup() => Root?.Dispose();
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: INVOCATION_COUNT)]
    public class InjectorGet : InjectorTestsBase
    {
        private sealed class DummyInterceptor : IInterfaceInterceptor
        {
            public object Invoke(IInvocationContext context, Next<object> callNext) => callNext();
        }

        public IInjector Injector { get; set; }

        public override void Cleanup()
        {
            if (!RootScope)
                Injector?.Dispose();
            base.Cleanup();
        }

        [Params(true, false)]
        public bool RootScope { get; set; }

        [ParamsSource(nameof(Lifetimes))]
        public LifetimeBase DependencyLifetime { get; set; }

        [ParamsSource(nameof(Lifetimes))]
        public LifetimeBase DependantLifetime { get; set; }

        [GlobalSetup(Target = nameof(NonGeneric))]
        public void SetupNonGeneric()
        {
            Setup(container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .Service<IDependant, Dependant>(DependantLifetime));
            Injector = RootScope ? (IInjector) Root : Root.CreateScope();
        }

        [Benchmark]
        public IDependant NonGeneric() => Injector.Get<IDependant>(name: null);

        [GlobalSetup(Target = nameof(NonGenericProxy))]
        public void SetupNonGenericProxy()
        {
            Setup(container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .Decorate<DummyInterceptor>()
                .Service<IDependant, Dependant>(DependantLifetime)
                .Decorate<DummyInterceptor>());
            Injector = RootScope ? (IInjector) Root : Root.CreateScope();
        }

        [Benchmark]
        public IDependant NonGenericProxy() => Injector.Get<IDependant>(name: null);

        [GlobalSetup(Target = nameof(Generic))]
        public void SetupGeneric()
        {
            Setup(container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .Service(typeof(IDependant<>), typeof(Dependant<>), DependantLifetime));
            Injector = RootScope ? (IInjector) Root : Root.CreateScope();
        }

        [Benchmark]
        public IDependant<string> Generic() => Injector.Get<IDependant<string>>(name: null);

        [GlobalSetup(Target = nameof(Lazy))]
        public void SetupLazy()
        {
            Setup(container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .Service<IDependantLazy, DependantLazy>(DependantLifetime));
            Injector = RootScope ? (IInjector)Root : Root.CreateScope();
        }

        [Benchmark]
        public IDependantLazy Lazy() => Injector.Get<IDependantLazy>(name: null);

        [GlobalSetup(Target = nameof(Enumerable))]
        public void SetupEnumerable()
        {
            Setup(container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .Service<IDependant, Dependant>(1.ToString(), DependantLifetime)
                .Service<IDependant, Dependant>(2.ToString(), DependantLifetime));
            Injector = RootScope ? (IInjector) Root : Root.CreateScope();
        }

        [Benchmark]
        public /*IEnumerable<IDependant>*/ void Enumerable() => Injector.Get<IEnumerable<IDependant>>(name: null);
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: INVOCATION_COUNT)]
    public class ScopeCreation : InjectorTestsBase
    {
        [GlobalSetup(Target = nameof(CreateAndDisposeScope))]
        public void SetupCreateInjector() => Setup(container => container
            .Service<IDependency, Dependency>(Lifetime.Transient)
            .Service<IDependant, Dependant>(Lifetime.Scoped));

        [Benchmark]
        public void CreateAndDisposeScope()
        {
            using (Root.CreateScope())
            {
            }
        }
    }
}
