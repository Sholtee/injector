/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using static Consts;
    using Internals;

    [MemoryDiagnoser]
    public class Injector
    {
        private IServiceContainer FContainer;

        #region UnsafeInjector
        private class UnsafeInjector : Internals.Injector
        {
            private void SupressOwnedEntryFinalization() 
            {
                foreach (AbstractServiceEntry entry in UnderlyingContainer.Where(e => e.Owner == this))
                {
                    GC.SuppressFinalize(entry);

                    switch (entry)
                    {
                        /*
                        case TransientServiceEntry transient: // IEnumerable<T>
                            GC.SuppressFinalize(transient.SpawnedServices);
                            break;
                        */
                        case InstanceServiceEntry instance: // IInjector
                            GC.SuppressFinalize(instance.Instance);
                            break;
                    }          
                }
            }

            private UnsafeInjector(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, ServiceGraph graph) : base(parent, factoryOptions, graph) 
                => SupressOwnedEntryFinalization();

            protected override void Dispose(bool disposeManaged)
            {
                UnsafeClear();
                base.Dispose(disposeManaged);
            }

            internal UnsafeInjector(IServiceContainer owner) : base(owner) => SupressOwnedEntryFinalization();

            internal override Internals.Injector Spawn(IServiceContainer parent)
            {
                var result = new UnsafeInjector(parent, FactoryOptions, FGraph.CreateSubgraph());
                GC.SuppressFinalize(result);
                return result;
            }

            internal override void Instantiate(ServiceReference requested)
            {
                GC.SuppressFinalize(requested);
                GC.SuppressFinalize(requested.Dependencies);

                GC.SuppressFinalize(requested.RelatedServiceEntry); // Genrikus lezarasakor bekerulhet uj bejegyzes ami konstruktorba meg nem volt
                if (requested.RelatedServiceEntry is TransientServiceEntry transient)
                    GC.SuppressFinalize(transient.SpawnedServices);

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

        [Params(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)]
        public Lifetime DependencyLifetime { get; set; }

        [Params(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)]
        public Lifetime DependantLifetime { get; set; }

        [GlobalSetup(Target = nameof(Get))]
        public void SetupGet() => FContainer = new DI.ServiceContainer()
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependant, Dependant>(DependantLifetime);

        [GlobalCleanup(Target = nameof(Get))]
        public void CleanupGet() => FContainer.Dispose();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Get()
        {
            //
            // "using" kell mindenkepp h a szulo kontenerunket ne arasszuk el nem hasznalt peldanyokkal
            // viszont az "UnsafeInjector.UnsafeClear()" hivas miatt nem tart sokaig.
            //

            using (var injector = new UnsafeInjector(FContainer))
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Get<IDependant>(name: null);
                }
            }
        }

        [GlobalSetup(Target = nameof(Get_Generic))]
        public void SetupGeneric() => FContainer = new DI.ServiceContainer()
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service(typeof(IDependant<>), typeof(Dependant<>), DependantLifetime);

        [GlobalCleanup(Target = nameof(Get_Generic))]
        public void CleanupGeneric() => FContainer.Dispose();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Get_Generic()
        {
            using (var injector = new UnsafeInjector(FContainer))
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Get<IDependant<string>>(name: null);
                }
            }
        }

        [GlobalSetup(Target = nameof(Get_Lazy))]
        public void SetupLazy() => FContainer = new DI.ServiceContainer()
            .Service<IDependency, Dependency>(DependencyLifetime)
            .Service<IDependantLazy, DependantLazy>(DependantLifetime);

        [GlobalCleanup(Target = nameof(Get_Lazy))]
        public void CleanupLazy() => FContainer.Dispose();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Get_Lazy()
        {
            using (var injector = new UnsafeInjector(FContainer))
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Get<IDependantLazy>(name: null);
                }
            }
        }

        [GlobalSetup(Target = nameof(Instantiate))]
        public void SetupInstantiate() => FContainer = new DI.ServiceContainer()
            .Service<IDependency, Dependency>(DependencyLifetime);

        [GlobalCleanup(Target = nameof(Instantiate))]
        public void CleanupInstantiate() => FContainer.Dispose();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Instantiate()
        {
            using (var injector = new UnsafeInjector(FContainer))
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
