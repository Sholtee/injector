/********************************************************************************
* IocComparison.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 30000)]
    public partial class IocComparison
    {
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
        #endregion

        public interface IIocContainer: IDisposable
        {
            IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation: TInterface;
            IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface;
            IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface;
            IIocContainer RegisterSingleton<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface: class;
            IIocContainer RegisterScoped<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface : class;
            IIocContainer RegisterTransient<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface : class;
            void Build();
            IDisposable CreateScope(out IServiceProvider provider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IDependant RequestService()
        {
            using (Container.CreateScope(out IServiceProvider serviceProvider))
            {
                return serviceProvider.GetService<IDependant>();
            }
        }

        public static IEnumerable<IIocContainer> Containers
        {
            get
            {
                yield return new InjectorDotNetServiceContainer();
                yield return new LamarContainer();
                yield return new MsExtensionsServiceCollection();
            }
        }

        [ParamsSource(nameof(Containers))]
        public IIocContainer Container { get; set; }

        [Params(false, true)]
        public bool UseFactory { get; set; }

        [GlobalCleanup]
        public void GlobalCleanup() => Container?.Dispose();

        [GlobalSetup(Target = nameof(SingletonService))]
        public void SetupSingletonService() => 
        (
            UseFactory
                ? Container
                    .RegisterSingleton<IDependency>(prov => new Dependency())
                    .RegisterSingleton<IDependant>(prov => new Dependant(prov.GetService<IDependency>()))
                : Container
                    .RegisterSingleton<IDependency, Dependency>()
                    .RegisterSingleton<IDependant, Dependant>()
        ).Build();

        [Benchmark]
        public IDependant SingletonService() => RequestService();

        [GlobalSetup(Target = nameof(ScopedService))]
        public void SetupScopedService() =>
        (
            UseFactory
                ? Container
                    .RegisterScoped<IDependency>(prov => new Dependency())
                    .RegisterScoped<IDependant>(prov => new Dependant(prov.GetService<IDependency>()))
                : Container
                    .RegisterScoped<IDependency, Dependency>()
                    .RegisterScoped<IDependant, Dependant>()
        ).Build();

        [Benchmark]
        public IDependant ScopedService() => RequestService();

        [GlobalSetup(Target = nameof(TransientService))]
        public void SetupTransientService() =>
        (
            UseFactory
                ? Container
                    .RegisterTransient<IDependency>(prov => new Dependency())
                    .RegisterTransient<IDependant>(prov => new Dependant(prov.GetService<IDependency>()))
                : Container
                    .RegisterTransient<IDependency, Dependency>()
                    .RegisterTransient<IDependant, Dependant>()
        ).Build();

        [Benchmark]
        public IDependant TransientService() => RequestService();
    }
}
