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
    using Internals;
    using Proxy;

    public class InjectorTestsBase
    {
        public const int INVOCATION_COUNT = 2000000;

        static InjectorTestsBase() =>
            //
            // Ugy tunik a modul inicializalok nem futnak ha a kodunkat a BenchmarkDotNet forditja
            //

            InjectorDotNetLifetime.Initialize();

        public static IEnumerable<Lifetime> Lifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
                yield return Lifetime.Singleton;
                yield return Lifetime.Pooled.Using(new PoolConfig(Capacity: 4));
            }
        }

        public static IEnumerable<string> Engines
        {
            get
            {
                yield return ServiceResolverLookup_Dict.Id;
                yield return ServiceResolverLookup_BTree.Id;
                yield return ServiceResolverLookup_BuiltBTree.Id;
            }
        }

        [ParamsSource(nameof(Engines))]
        public string Engine { get; set; }

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

        protected IScopeFactory Setup(Action<IServiceCollection> setupContainer) => Root = ScopeFactory.Create(setupContainer, new ScopeOptions { Engine = Engine, ServiceResolutionMode = ResolutionMode });

        [GlobalCleanup]
        public virtual void Cleanup() => Root?.Dispose();
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: INVOCATION_COUNT)]
    public class InjectorGet : InjectorTestsBase
    {
        public IInjector Injector { get; set; }

        public override void Cleanup()
        {
            Injector?.Dispose();
            base.Cleanup();
        }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependencyLifetime { get; set; }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependantLifetime { get; set; }

        [GlobalSetup(Target = nameof(NonGeneric))]
        public void SetupNonGeneric() => Injector = Setup
        (
            container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .Service<IDependant, Dependant>(DependantLifetime)
        ).CreateScope();

        [Benchmark]
        public IDependant NonGeneric() => Injector.Get<IDependant>(name: null);

        [GlobalSetup(Target = nameof(NonGenericProxy))]
        public void SetupNonGenericProxy() => Injector = Setup
        (
            container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .WithProxy<InterfaceInterceptor<IDependency>>()
                .Service<IDependant, Dependant>(DependantLifetime)
                .WithProxy<InterfaceInterceptor<IDependant>>()
        ).CreateScope();

        [Benchmark]
        public IDependant NonGenericProxy() => Injector.Get<IDependant>(name: null);

        [GlobalSetup(Target = nameof(Generic))]
        public void SetupGeneric() => Injector = Setup
        (
            container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .Service(typeof(IDependant<>), typeof(Dependant<>), DependantLifetime)
        ).CreateScope();

        [Benchmark]
        public IDependant<string> Generic() => Injector.Get<IDependant<string>>(name: null);

        [GlobalSetup(Target = nameof(Lazy))]
        public void SetupLazy() => Injector = Setup
        (
            container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .Service<IDependantLazy, DependantLazy>(DependantLifetime)
        ).CreateScope();

        [Benchmark]
        public IDependantLazy Lazy() => Injector.Get<IDependantLazy>(name: null);

        [GlobalSetup(Target = nameof(Enumerable))]
        public void SetupEnumerable() => Injector = Setup
        (
            container => container
                .Service<IDependency, Dependency>(DependencyLifetime)
                .Service<IDependant, Dependant>(1.ToString(), DependantLifetime)
                .Service<IDependant, Dependant>(2.ToString(), DependantLifetime)
        ).CreateScope();

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
