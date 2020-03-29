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
        private DI.ServiceContainer FContainer;

        [GlobalCleanup]
        public void Cleanup()
        {
            FContainer?.Dispose();
            FContainer = null;
        }

        [GlobalSetup(Target = nameof(Add))]
        public void SetupAdd() => FContainer = new DI.ServiceContainer();

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
            for (int i = 0; i < OperationsPerInvoke; i++, OverallAddInvocations++)
            {
                //
                // Absztrakt bejegyzesnek nincs tulaja aki felszabaditsa (megjegyzem nem is kell felszabaditani).
                //

                var entry = new AbstractServiceEntry(RandomInterfaces[OverallAddInvocations % RandomInterfaces.Count], (OverallAddInvocations / RandomInterfaces.Count).ToString());
                GC.SuppressFinalize(entry);

                FContainer.Add(entry);
            }

            FContainer.UnsafeClear(); // enelkul siman lehet hash utkozest 
        }

        [GlobalSetup(Target = nameof(Get))]
        public void SetupGet() 
        {
            FContainer = new DI.ServiceContainer();
            FContainer.Factory<IList>(injector => Array.Empty<int>());
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
                FContainer.Service(typeof(IList<>), i.ToString(), typeof(MyList<>));
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
