/********************************************************************************
* ExperimentalServiceRegistry.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    public abstract class ExperimentalServiceRegistryBase
    {
        [Params(1, 5, 10)]
        public int ServiceCount { get; set; }

        private protected ExperimentalServiceRegistry Registry { get; set; }

        [GlobalCleanup]
        public virtual void Cleanup() => Registry.Dispose();
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: INVOCATION_COUNT)]
    public class ExperimentalServiceRegistry_GetEntry : ExperimentalServiceRegistryBase
    {
        public const int INVOCATION_COUNT = 5000;

        public const int OPERATIONS_PER_INVOKE = 1000;

        private int I;

        [GlobalSetup(Target = nameof(ResolveRegularEntry))]
        public void SetupResolveRegularEntry()
        {
            ServiceCollection services = new();

            for (int i = 0; i < ServiceCount; i++)
            {
                services.Factory<IList>(i.ToString(), _ => Array.Empty<object>(), Lifetime.Transient);
            }

            Registry = new ExperimentalServiceRegistry(services);
            I = 0;
        }

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void ResolveRegularEntry()
        {
            string name = (++I % ServiceCount).ToString();
            Type svc = typeof(IList);

            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                _ = Registry!.ResolveEntry(svc, name);
            }
        }

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

            Registry = new ExperimentalServiceRegistry(services);
            I = 0;
        }

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void ResolveGenericEntry()
        {
            string name = (++I % ServiceCount).ToString();
            Type svc = typeof(IList<object>);

            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                _ = Registry!.ResolveEntry(svc, name);
            }
        }
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 1000000)]
    public class ExperimentalServiceRegistry_Derive : ExperimentalServiceRegistryBase
    {
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

            Registry = new ExperimentalServiceRegistry(services);
        }

        [Benchmark]
        public void DeriveAndDispose()
        {
            using (ExperimentalServiceRegistry child = new ExperimentalServiceRegistry(Registry))
            {
                _ = child.Parent;
            }
        }

        [Benchmark]
        public async Task DeriveAndDisposeAsync()
        {
            await using (ExperimentalServiceRegistry child = new ExperimentalServiceRegistry(Registry))
            {
                _ = child.Parent;
            }
        }
    }
}
