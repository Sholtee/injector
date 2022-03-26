/********************************************************************************
* ExperimentalServiceRegistry.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Concurrent;
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

        public const int OPERATIONS_PER_INVOKE_SMALL = 10;

        private int I;

        private void SetupResolveRegularEntryCore()
        {
            ServiceCollection services = new();

            for (int i = 0; i < ServiceCount; i++)
            {
                services.Factory<IList>(i.ToString(), _ => Array.Empty<object>(), Lifetime.Transient);
            }

            Registry = new ExperimentalServiceRegistry(services);
            I = 0;
        }

        [GlobalSetup(Target = nameof(ResolveRegularEntry))]
        public void SetupResolveRegularEntry() => SetupResolveRegularEntryCore();

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

        [GlobalSetup(Target = nameof(ResolveRegularEntryFromChild))]
        public void SetupResolveRegularEntryFromChild() => SetupResolveRegularEntryCore();

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void ResolveRegularEntryFromChild()
        {
            string name = (++I % ServiceCount).ToString();
            Type svc = typeof(IList);

            ExperimentalServiceRegistry child = new(Registry);

            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                _ = child.ResolveEntry(svc, name);
            }
        }

        [GlobalSetup(Target = nameof(ResolveRegularEntryFromChildSingleCall))]
        public void SetupResolveRegularEntryFromChildSingleCall() => SetupResolveRegularEntryCore();

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE_SMALL)]
        public void ResolveRegularEntryFromChildSingleCall()
        {
            for (int i = 0; i < OPERATIONS_PER_INVOKE_SMALL; i++)
            {
                _ = new ExperimentalServiceRegistry(Registry).ResolveEntry(typeof(IList), (++I % ServiceCount).ToString());
            }
        }

        private sealed class MyList<T> : List<T> // csak egy konstruktora van
        {
        }

        private void SetupResolveGenericEntryCore()
        {
            ServiceCollection services = new();

            for (int i = 0; i < ServiceCount; i++)
            {
                services.Service(typeof(IList<>), i.ToString(), typeof(MyList<>), Lifetime.Transient);
            }

            Registry = new ExperimentalServiceRegistry(services);
            I = 0;
        }

        [GlobalSetup(Target = nameof(ResolveGenericEntry))]
        public void SetupResolveGenericEntry() => SetupResolveGenericEntryCore();

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

        [GlobalSetup(Target = nameof(ResolveGenericEntryFromChild))]
        public void SetupResolveGenericEntryFromChild() => SetupResolveGenericEntryCore();

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void ResolveGenericEntryFromChild()
        {
            string name = (++I % ServiceCount).ToString();
            Type svc = typeof(IList<object>);

            ExperimentalServiceRegistry child = new(Registry);

            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                _ = child.ResolveEntry(svc, name);
            }
        }

        [GlobalSetup(Target = nameof(ResolveGenericEntryFromChildSingleCall))]
        public void SetupResolveGenericEntryFromChildSingleCall() => SetupResolveGenericEntryCore();

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE_SMALL)]
        public void ResolveGenericEntryFromChildSingleCall()
        {
            for (int i = 0; i < OPERATIONS_PER_INVOKE_SMALL; i++)
            {
                _ = new ExperimentalServiceRegistry(Registry).ResolveEntry(typeof(IList<object>), (++I % ServiceCount).ToString());
            }
        }
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 1000000)]
    public class ExperimentalServiceRegistry_Derive : ExperimentalServiceRegistryBase
    {
        [GlobalSetup]
        public void Setup()
        {
            ServiceCollection services = new();

            for (int i = 0; i < 10; i++)
            {
                services.Service<IDisposable, Disposable>(i.ToString(), Lifetime.Scoped);
            }

            Registry = new ExperimentalServiceRegistry(services);
        }

        [Benchmark]
        public void DeriveAndDispose()
        {
            using (ExperimentalServiceRegistry child = new(Registry))
            {
                _ = child.Parent;
            }
        }

        [Benchmark]
        public async Task DeriveAndDisposeAsync()
        {
            await using (ExperimentalServiceRegistry child = new(Registry))
            {
                _ = child.Parent;
            }
        }
    }
}
