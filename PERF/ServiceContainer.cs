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
    using Interfaces;

    [MemoryDiagnoser]
    public class ServiceContainer
    {
        private DI.ServiceContainer FContainer;

        [GlobalCleanup]
        public void Cleanup()
        {
            FContainer?.Dispose();
            FContainer = null;
        }

        private static readonly IReadOnlyList<Type> RandomInterfaces = typeof(object)
            .Assembly
            .GetTypes()
            .Where(t => t.IsInterface)
            .ToArray();

        //
        // FContainer.Count lekerdezese idoigenyes
        //

        private int OverallAddInvocations { get; set; }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Add() 
        {
            using (IServiceContainer container = new DI.ServiceContainer())
            {
                for (int i = 0; i < OperationsPerInvoke; i++, OverallAddInvocations++)
                {
                    container.Add
                    (
                        new AbstractServiceEntry
                        (
                            RandomInterfaces[OverallAddInvocations % RandomInterfaces.Count],
                            (OverallAddInvocations / RandomInterfaces.Count).ToString(),
                            container
                        )
                    );
                }
            }
        }

        [GlobalSetup(Target = nameof(Get))]
        public void SetupGet() 
        {
            FContainer = new DI.ServiceContainer();
            FContainer.Factory<IList>(injector => Array.Empty<int>(), Lifetime.Transient);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Get()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FContainer.Get<IList>(QueryModes.ThrowOnError);
            }
        }

        [GlobalSetup(Target = nameof(Specialize))]
        public void SetupSpecialize()
        {
            FContainer = new DI.ServiceContainer();
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FContainer.Service(typeof(IList<>), i.ToString(), typeof(MyList<>), Lifetime.Transient);
            }
        }

        private sealed class MyList<T> : List<T> { } // csak egy konstruktor legyen

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Specialize() 
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FContainer.Get<IList<object>>(i.ToString(), QueryModes.AllowSpecialization | QueryModes.ThrowOnError);
            }
        }
    }
}
