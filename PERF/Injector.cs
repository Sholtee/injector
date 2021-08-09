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

    public class InjectorTestsBase
    {
        static InjectorTestsBase() =>
            //
            // Ugy tunik a modul inicializalok nem futnak ha a kodunkat a BenchmarkDotNet forditja
            //

            InjectorDotNetLifetime.Initialize();

        public IEnumerable<Lifetime> Lifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
                yield return Lifetime.Singleton;
                yield return Lifetime.Pooled.WithCapacity(4);
            }
        }

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

        public class Outer
        {
            public Outer(IDependency dependency, int num) => Dependency = dependency;
            public IDependency Dependency { get; }
        }
        #endregion

        protected IServiceContainer Container { get; private set; }

        protected void Setup(Action<IServiceContainer> setupContainer, int maxSpawnedTransientServices = int.MaxValue, int maxChildCount = int.MaxValue)
        {
            Config.Value.Injector.MaxSpawnedTransientServices = maxSpawnedTransientServices;
            Config.Value.ServiceContainer.MaxChildCount = maxChildCount;

            Container = new DI.ServiceContainer();
            setupContainer(Container);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Container?.Dispose();
            Config.Reset();
        }
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000)]
    public class InjectorGet: InjectorTestsBase
    {
        public IInjector Injector { get; set; }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependencyLifetime { get; set; }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependantLifetime { get; set; }

        [GlobalSetup(Target = nameof(NonGeneric))]
        public void SetupNonGeneric() => Setup(container => Injector = container
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependant, Dependant>(DependantLifetime)
            .CreateInjector());

        [Benchmark]
        public IDependant NonGeneric() => Injector.Get<IDependant>(name: null);

        [GlobalSetup(Target = nameof(Generic))]
        public void SetupGeneric() => Setup(container => Injector = container
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service(typeof(IDependant<>), typeof(Dependant<>), DependantLifetime)
            .CreateInjector());

        [Benchmark]
        public IDependant<string> Generic() => Injector.Get<IDependant<string>>(name: null);

        [GlobalSetup(Target = nameof(Lazy))]
        public void SetupLazy() => Setup(container => Injector = container
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependantLazy, DependantLazy>(DependantLifetime)
            .CreateInjector());

        [Benchmark]
        public IDependantLazy Lazy() => Injector.Get<IDependantLazy>(name: null);

        [GlobalSetup(Target = nameof(Enumerable))]
        public void SetupEnumerable() => Setup(container => Injector = container
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependant, Dependant>(1.ToString(), DependantLifetime)
            .Service<IDependant, Dependant>(2.ToString(), DependantLifetime)
            .CreateInjector());

        [Benchmark]
        public /*IEnumerable<IDependant>*/ void Enumerable() => Injector.Get<IEnumerable<IDependant>>(name: null);
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000)]
    public class InjectorInstantiate : InjectorTestsBase 
    {
        public IInjector Injector { get; set; }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependencyLifetime { get; set; }

        [GlobalSetup(Target = nameof(Instantiate))]
        public void SetupInstantiate() => Setup(container => Injector = container
            .Service<IDependency, Dependency>(DependencyLifetime)
            .CreateInjector());

        [Benchmark]
        public Outer Instantiate() => Injector.Instantiate<Outer>(new Dictionary<string, object>
        {
            { "num", 10 }
        });
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000)]
    public class InjectorLifetime : InjectorTestsBase
    {
        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependencyLifetime { get; set; }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependantLifetime { get; set; }

        [GlobalSetup(Target = nameof(CreateInjector))]
        public void SetupCreateInjector() => Setup(container => container
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependant, Dependant>(DependantLifetime));

        [Benchmark]
        public IInjector CreateInjector() => Container.CreateInjector();
    }
}
