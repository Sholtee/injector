/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;
    using Primitives;
    using Primitives.Patterns;

    public abstract class ServiceRegistryTestsBase
    {
        public const int INVOCATION_COUNT = 1000000;

        public sealed class Named<T>
        {
            public string Name { get; init; }
            public T Value { get; init; }
            public override string ToString() => Name;
        }

        public static IEnumerable<Named<Type>> RegistryTypes
        {
            get
            {
                yield return new Named<Type> { Name = "Linear", Value = typeof(ServiceRegistry) };
                yield return new Named<Type> { Name = "Concurrent", Value = typeof(ConcurrentServiceRegistry) };
            }
        }

        //
        // Mind az deklaralo osztalynak mind a ParameterSource-al annotalt property-nek publikusnak kell lennie... Viszont a ResolverBuilder
        // internal, ezert object-et adunk vissza.
        //

        public static IEnumerable<Named<object>> ResolverBuilders
        {
            get
            {
                yield return new Named<object> { Name = "Chained", Value = ResolverBuilder.ChainedDelegates };
                yield return new Named<object> { Name = "Expression", Value = ResolverBuilder.CompiledExpression };
                yield return new Named<object> { Name = "Compiled", Value = ResolverBuilder.CompiledCode };
            }
        }

        [ParamsSource(nameof(RegistryTypes))]
        public Named<Type> RegistryType { get; set; }
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: INVOCATION_COUNT)]
    public class ServiceRegistry_GetEntry: ServiceRegistryTestsBase
    {
        [ParamsSource(nameof(ResolverBuilders))]
        public Named<object> ResolverBuilder { get; set; }

        [Params(1, 5, 10)]
        public int ServiceCount { get; set; }

        private ServiceRegistryBase Registry { get; set; }

        private int I { get; set; }

        [GlobalCleanup]
        public void Cleanup() => Registry.Dispose();

        [GlobalSetup(Target = nameof(ResolveRegularEntry))]
        public void SetupResolveRegularEntry()
        {
            ServiceCollection services = new();

            for (int i = 0; i < ServiceCount; i++)
            {
                services.Factory<IList>(i.ToString(), _ => Array.Empty<object>(), Lifetime.Transient);
            }

            Registry = (ServiceRegistryBase) System.Activator.CreateInstance(RegistryType.Value, new object[] 
            {
                services,
                ResolverBuilder.Value,
                CancellationToken.None
            });
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry ResolveRegularEntry() => Registry!.GetEntry(typeof(IList), (++I % ServiceCount).ToString())!;

        private sealed class MyList<T> : List<T> // csak egy konstruktora van
        {
        }

        [GlobalSetup(Target = nameof(ResolveGenericEntry))]
        public void SetupResolveGenericEntry()
        {
            ServiceCollection services = new();

            for (int i = 0; i < ServiceCount; i++)
            {
                services.Service(typeof(IList<>), i.ToString(), typeof(MyList<>), Lifetime.Transient);
            }

            Registry = (ServiceRegistryBase) System.Activator.CreateInstance(RegistryType.Value, new object[]
            {
                services,
                ResolverBuilder.Value,
                CancellationToken.None
            });
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry ResolveGenericEntry() => Registry!.GetEntry(typeof(IList<object>), (++I % ServiceCount).ToString())!;
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: INVOCATION_COUNT)]
    public class ServiceRegistry_Derive : ServiceRegistryTestsBase
    {
        private ServiceRegistryBase Registry { get; set; }

        private Func<object[], object> Factory { get; set; }

        [Params(0, 1, 5, 10)]
        public int EntryCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            ServiceCollection services = new();

            for (int i = 0; i < EntryCount; i++)
            {
                services.Service<IDisposable, Disposable>(i.ToString(), Lifetime.Scoped);
            }

            Registry = (ServiceRegistryBase) System.Activator.CreateInstance(RegistryType.Value, new object[]
            {
                services,
                ResolverBuilder.CompiledExpression,
                CancellationToken.None
            });

            Factory = (RegistryType.Value.GetConstructor(new Type[] { RegistryType.Value }) ?? RegistryType.Value.GetConstructor(new Type[] { typeof(ServiceRegistryBase) }))
                .ToStaticDelegate();
        }

        [GlobalCleanup]
        public void Cleanup() => Registry?.Dispose();

        [Benchmark]
        public void DeriveAndDispose()
        {
            using (IServiceRegistry child = (IServiceRegistry) Factory(new object[] { Registry }))
            {
                _ = child.Parent;
            }
        }

        [Benchmark]
        public async Task DeriveAndDisposeAsync()
        {
            await using (IServiceRegistry child = (IServiceRegistry) Factory(new object[] { Registry }))
            {
                _ = child.Parent;
            }
        }
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 100)] // ez hivhatja a Roslyn-t stb szoval nem lesz epp gyors
    public class ServiceRegistry_Create : ServiceRegistryTestsBase
    {
        [ParamsSource(nameof(ResolverBuilders))]
        public Named<object> ResolverBuilder { get; set; }

        private Func<object[], object> Factory { get; set; }

        private IServiceCollection EmptyColl { get; } = new ServiceCollection();

        [GlobalSetup]
        public void Setup() => Factory = RegistryType
            .Value
            .GetConstructor(new Type[]
            {
                typeof(IServiceCollection),
                typeof(ResolverBuilder),
                typeof(CancellationToken)
            })
            .ToStaticDelegate();

        [Benchmark]
        public void CreateAndDispose()
        {
            using (IServiceRegistry registry = (IServiceRegistry) Factory(new object[] { EmptyColl, ResolverBuilder.Value, CancellationToken.None })) 
            {
                _ = registry.Parent;
            }
        }
    }
}
