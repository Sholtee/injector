/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using static Consts;

    [MemoryDiagnoser]
    public class ServiceContainer
    {
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Add() 
        {
            using (var container = new DI.ServiceContainer())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    container.Factory<IDisposable>(i.ToString(), injector => null);
                }
            }
        }

        private IServiceContainer FContainer;

        [GlobalSetup(Target = nameof(Get))]
        public void SetupGet() 
        {
            FContainer = new DI.ServiceContainer();
            FContainer.Factory<IList>(injector => Array.Empty<int>());
        }

        [GlobalCleanup]
        public void Cleanup() 
        {
            FContainer?.Dispose();
            FContainer = null;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Get()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FContainer.Get<IList>();
            }
        }

        [GlobalSetup(Target = nameof(Specialize))]
        public void SetupSpecialize()
        {
            FContainer = new DI.ServiceContainer();
            FContainer.Service(typeof(IList<>), typeof(MyList<>));
        }

        private sealed class MyList<T> : List<T> { } // csak egy konstruktor legyen

        private static readonly IReadOnlyList<Type> RandomTypes = typeof(System.Xml.NameTable)
            .Assembly
            .GetTypes()
            .Where(t => t.IsPublic && !t.IsGenericTypeDefinition)
            .Select(t => typeof(IList<>).MakeGenericType(t))
            .ToArray();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Specialize() 
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FContainer.Get(RandomTypes[Math.Min(i, RandomTypes.Count - 1)]);
            }
        }
    }
}
