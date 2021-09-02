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

using Lamar;
using Microsoft.Extensions.DependencyInjection;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Primitives.Patterns;

    using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;

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
            void Build();
            IServiceProvider CreateScope();
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

            public void Build() { }

            public IServiceProvider CreateScope()
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

        public sealed class LamarContainer : Disposable,  IIocContainer
        {
            private readonly IServiceCollection FUnderlyingContainer = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            private Container FBuiltContainer;

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FBuiltContainer?.Dispose();

                base.Dispose(disposeManaged);
            }

            public void Build() => FBuiltContainer = new Container(FUnderlyingContainer);

            public IServiceProvider CreateScope() => FBuiltContainer.GetNestedContainer();

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.AddScoped(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.AddSingleton(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.AddTransient(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public override string ToString() => nameof(LamarContainer);
        }

        public sealed class MsExtensionsServiceCollection : Disposable, IIocContainer
        {
            private readonly IServiceCollection FUnderlyingContainer = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            private ServiceProvider FBuiltProvider;

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FBuiltProvider?.Dispose();

                base.Dispose(disposeManaged);
            }

            public void Build() => FBuiltProvider = FUnderlyingContainer.BuildServiceProvider();

            public IServiceProvider CreateScope() => FBuiltProvider.CreateScope().ServiceProvider;

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.AddScoped(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.AddSingleton(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.AddTransient(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public override string ToString() => nameof(MsExtensionsServiceCollection);
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IDependant RequestService()
        {
            IServiceProvider serviceProvider = Container.CreateScope();
            try
            {
                return serviceProvider.GetService<IDependant>();
            }
            finally
            {
                (serviceProvider as IDisposable)?.Dispose();
            }
        }

        public IEnumerable<IIocContainer> Containers
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

        [GlobalCleanup]
        public void GlobalCleanup() => (Container as IDisposable)?.Dispose();

        [GlobalSetup(Target = nameof(Singleton))]
        public void SetupSingleton() => Container
            .RegisterSingleton<IDependency, Dependency>()
            .RegisterSingleton<IDependant, Dependant>()
            .Build();

        [Benchmark]
        public IDependant Singleton() => RequestService();

        [GlobalSetup(Target = nameof(Scoped))]
        public void SetupScoped() => Container
            .RegisterScoped<IDependency, Dependency>()
            .RegisterScoped<IDependant, Dependant>()
            .Build();

        [Benchmark]
        public IDependant Scoped() => RequestService();

        [GlobalSetup(Target = nameof(Transient))]
        public void SetupTransient() => Container
            .RegisterTransient<IDependency, Dependency>()
            .RegisterTransient<IDependant, Dependant>()
            .Build();

        [Benchmark]
        public IDependant Transient() => RequestService();
    }
}
