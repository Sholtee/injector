/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using static Consts;
    using Interfaces;
    using Internals;

    public class InjectorTestsBase
    {
        static InjectorTestsBase() 
        {
            //
            // Ugy tunik a modul inicializalok nem futnak ha a kodunkat a BenchmarkDotNet forditja
            //

            InstanceLifetime.Setup();
            SingletonLifetime.Setup();
            TransientLifetime.Setup();
            ScopedLifetime.Setup();
            PooledLifetime.Setup();
        }

        private IServiceContainer FContainer;

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

        protected IServiceContainer CreateContainer() => FContainer = new DI.ServiceContainer();

        protected IInjector CreateInjector() => FContainer.CreateInjector();

        [GlobalCleanup]
        public void Cleanup() => FContainer.Dispose();
    }

    [MemoryDiagnoser]
    public class InjectorGet: InjectorTestsBase
    {
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

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependencyLifetime { get; set; }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependantLifetime { get; set; }

        [GlobalSetup(Target = nameof(NonGeneric))]
        public void SetupNonGeneric() => CreateContainer()
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependant, Dependant>(DependantLifetime);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void NonGeneric()
        {
            using (IInjector injector = CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Get<IDependant>(name: null);
                }
            }
        }

        [GlobalSetup(Target = nameof(Generic))]
        public void SetupGeneric() => CreateContainer()
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service(typeof(IDependant<>), typeof(Dependant<>), DependantLifetime);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Generic()
        {
            using (IInjector injector = CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Get<IDependant<string>>(name: null);
                }
            }
        }

        [GlobalSetup(Target = nameof(Lazy))]
        public void SetupLazy() => CreateContainer()
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependantLazy, DependantLazy>(DependantLifetime);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Lazy()
        {
            using (IInjector injector = CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Get<IDependantLazy>(name: null);
                }
            }
        }

        [GlobalSetup(Target = nameof(Enumerable))]
        public void SetupEnumerable() => CreateContainer()
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependant, Dependant>(1.ToString(), DependantLifetime)
            .Service<IDependant, Dependant>(2.ToString(), DependantLifetime);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Enumerable()
        {
            using (IInjector injector = CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Get<IEnumerable<IDependant>>(name: null);
                }
            }
        }
    }

    [MemoryDiagnoser]
    public class InjectorInstantiate : InjectorTestsBase 
    {
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

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime DependencyLifetime { get; set; }

        [GlobalSetup(Target = nameof(Instantiate))]
        public void SetupInstantiate() => CreateContainer()
            .Service<IDependency, Dependency>(DependencyLifetime);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Instantiate()
        {
            using (IInjector injector = CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Instantiate<Outer>(new Dictionary<string, object>
                    {
                        { "num", 10 }
                    });
                }
            }
        }
    }
}
