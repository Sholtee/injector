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
    using Internals;

    public class InjectorTestsBase
    {
        private IServiceContainer FContainer;

        #region UnsafeInjector
        private class UnsafeInjector : Internals.Injector
        {
            private UnsafeInjector(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, ServiceGraph graph) : base(parent, factoryOptions, graph) { }

            protected override void Dispose(bool disposeManaged)
            {
                UnsafeClear();
                base.Dispose(disposeManaged);
            }

            public override IServiceContainer Add(AbstractServiceEntry entry)
            {
                if (entry.Owner == this)
                {
                    switch (entry)
                    {
                        case TransientServiceEntry transient:
                            GC.SuppressFinalize(transient.SpawnedServices);
                            break;
                        case InstanceServiceEntry instance:
                            GC.SuppressFinalize(instance.Instance);
                            break;
                    }

                    GC.SuppressFinalize(entry);
                }

                return base.Add(entry);
            }

            internal UnsafeInjector(IServiceContainer owner) : base(owner) { }

            internal override Internals.Injector Spawn(IServiceContainer parent)
            {
                var result = new UnsafeInjector(parent, FactoryOptions, CreateSubgraph());
                GC.SuppressFinalize(result);
                return result;
            }

            internal override void Instantiate(ServiceReference requested)
            {
                GC.SuppressFinalize(requested);
                GC.SuppressFinalize(requested.Dependencies);

                base.Instantiate(requested);
            }
        }
        #endregion

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

        protected IInjector CreateInjector() => new UnsafeInjector(FContainer);

        [GlobalCleanup]
        public void Cleanup() => FContainer.Dispose();
    }

    [MemoryDiagnoser]
    public class InjectorGet: InjectorTestsBase
    {
        [Params(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)]
        public Lifetime DependencyLifetime { get; set; }

        [Params(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)]
        public Lifetime DependantLifetime { get; set; }

        [GlobalSetup(Target = nameof(NonGeneric))]
        public void SetupNonGeneric() => CreateContainer()
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependant, Dependant>(DependantLifetime);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void NonGeneric()
        {
            //
            // "using" kell mindenkepp h a szulo kontenerunket ne arasszuk el nem hasznalt peldanyokkal
            // viszont az "UnsafeInjector.UnsafeClear()" hivas miatt nem tart sokaig.
            //

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
        [Params(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)]
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
