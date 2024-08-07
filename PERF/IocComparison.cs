﻿/********************************************************************************
* IocComparison.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 100000)]
    public partial class IocComparison
    {
        private static string GetComponentName(Type t)
        {
            AssemblyName asmName = t.Assembly.GetName();
            return $"{asmName.Name} {asmName.Version}";
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
        #endregion

        [Params(0, 1, 5, 20)]
        public int InvocationCount { get; set; }

        public interface IIocContainer: IDisposable
        {
            IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation: TInterface;
            IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface;
            IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface;
            void Build();
            IDisposable CreateScope(out IServiceProvider provider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RequestService()
        {
            using (Container.CreateScope(out IServiceProvider serviceProvider))
            {
                for (int i = 0; i < InvocationCount; i++)
                {
                    _ = serviceProvider.GetService<IDependant>();
                }
            }
        }

        public static IEnumerable<IIocContainer> Containers
        {
            get
            {
                yield return new Autofac();
                yield return new DryIoc();
                yield return new InjectorDotNet();
                yield return new Lamar();
                yield return new MsExtensionsDI();
                yield return new Stashbox();
            }
        }

        [ParamsSource(nameof(Containers))]
        public IIocContainer Container { get; set; }

        [GlobalCleanup]
        public void GlobalCleanup() => Container?.Dispose();

        [GlobalSetup(Target = nameof(SingletonService))]
        public void SetupSingletonService() => Container
            .RegisterSingleton<IDependency, Dependency>()
            .RegisterSingleton<IDependant, Dependant>()
            .Build();

        [Benchmark]
        public void SingletonService() => RequestService();

        [GlobalSetup(Target = nameof(ScopedService))]
        public void SetupScopedService() => Container
            .RegisterScoped<IDependency, Dependency>()
            .RegisterScoped<IDependant, Dependant>().Build();

        [Benchmark]
        public void ScopedService() => RequestService();

        [GlobalSetup(Target = nameof(TransientService))]
        public void SetupTransientService() => Container
            .RegisterTransient<IDependency, Dependency>()
            .RegisterTransient<IDependant, Dependant>().Build();

        [Benchmark]
        public void TransientService() => RequestService();
    }
}
