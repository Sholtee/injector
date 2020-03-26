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

        private int OverallCount { get; set; }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Add() 
        {
            for (int i = 0; i < OperationsPerInvoke; i++, OverallCount++)
            {
                //
                // Absztrakt bejegyzesnek nincs tulaja aki felszabaditsa (megjegyzem nem is kell felszabaditani).
                //

                var entry = new AbstractServiceEntry(RandomInterfaces[OverallCount % RandomInterfaces.Count], (OverallCount / RandomInterfaces.Count).ToString());
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
            FContainer.Service(typeof(IList<>), typeof(MyList<>));
        }

        private sealed class MyList<T> : List<T> { } // csak egy konstruktor legyen

        private static readonly IReadOnlyList<Type> RandomLists = typeof(System.Xml.NameTable)
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
                FContainer.Get(RandomLists[Math.Min(i, RandomLists.Count - 1)], null, QueryModes.AllowSpecialization | QueryModes.ThrowOnError);
            }
        }
    }
}
