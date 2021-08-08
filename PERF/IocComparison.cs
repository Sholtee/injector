/********************************************************************************
* IocComparison.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

using Microsoft.Extensions.DependencyInjection;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Primitives.Patterns;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000)]
    public class IocComparison
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

        public interface IIocContainer
        {
            IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation: TInterface;
            IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface;
            IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface;
            IServiceProvider CreateProvider();
        }

        #region Containers
        public sealed class InjectorDotNetServiceContainer : Disposable, IIocContainer
        {
            private readonly IServiceContainer FUnderlyingContainer = new DI.ServiceContainer();

            public InjectorDotNetServiceContainer()
            {
                Internals.Config.Value.Injector.MaxSpawnedTransientServices = int.MaxValue;
            }

            protected override void Dispose(bool disposeManaged)
            {
                FUnderlyingContainer.Dispose();        
                Internals.Config.Reset();

                base.Dispose(disposeManaged);
            }

            public IServiceProvider CreateProvider()
            {
                FUnderlyingContainer.CreateProvider(out IServiceProvider serviceProvider);
                return serviceProvider;
            }

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.Service(typeof(TInterface), typeof(TImplementation), Lifetime.Scoped);
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.Service(typeof(TInterface), typeof(TImplementation), Lifetime.Singleton);
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.Service(typeof(TInterface), typeof(TImplementation), Lifetime.Transient);
                return this;
            }

            public override string ToString() => nameof(InjectorDotNetServiceContainer);
        }

        public sealed class MsExtensionsServiceCollection : IIocContainer
        {
            private readonly IServiceCollection FunderlyingContainer = new ServiceCollection();

            public IServiceProvider CreateProvider() => FunderlyingContainer.BuildServiceProvider();

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FunderlyingContainer.AddScoped(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FunderlyingContainer.AddSingleton(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FunderlyingContainer.AddTransient(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public override string ToString() => nameof(MsExtensionsServiceCollection);
        }
        #endregion

        public IServiceProvider ServiceProvider { get; set; }

        public IEnumerable<IIocContainer> Containers
        {
            get
            {
                yield return new InjectorDotNetServiceContainer();
                yield return new MsExtensionsServiceCollection();
            }
        }

        [ParamsSource(nameof(Containers))]
        public IIocContainer Container { get; set; }

        [GlobalCleanup]
        public void GlobalCleanup() => (Container as IDisposable)?.Dispose();

        [GlobalSetup(Target = nameof(Singleton))]
        public void SetupSingleton() => ServiceProvider = Container
            .RegisterSingleton<IDependency, Dependency>()
            .RegisterSingleton<IDependant, Dependant>()
            .CreateProvider();

        [GlobalCleanup(Target = nameof(Singleton))]
        public void CleanupSingleton() => ((IDisposable) ServiceProvider).Dispose();

        [Benchmark]
        public IDependant Singleton() => ServiceProvider.GetService<IDependant>();

        [GlobalSetup(Target = nameof(Scoped))]
        public void SetupScoped() => ServiceProvider = Container
            .RegisterScoped<IDependency, Dependency>()
            .RegisterScoped<IDependant, Dependant>()
            .CreateProvider();

        [GlobalCleanup(Target = nameof(Scoped))]
        public void CleanupScoped() => ((IDisposable) ServiceProvider).Dispose();

        [Benchmark]
        public IDependant Scoped() => ServiceProvider.GetService<IDependant>();

        [GlobalSetup(Target = nameof(Transient))]
        public void SetupTransient() => ServiceProvider = Container
            .RegisterTransient<IDependency, Dependency>()
            .RegisterTransient<IDependant, Dependant>()
            .CreateProvider();

        [GlobalCleanup(Target = nameof(Transient))]
        public void CleanupTransient() => ((IDisposable) ServiceProvider).Dispose();

        [Benchmark]
        public IDependant Transient() => ServiceProvider.GetService<IDependant>();
    }
}
