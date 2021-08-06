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
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: InvocationCount)]
    public class ServiceContainer
    {
        const int InvocationCount = 10000;

        private int I;

        private static readonly IReadOnlyList<Type> RandomClasses = typeof(object)
            .Assembly
            .ExportedTypes
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .ToArray();

        private static readonly IReadOnlyList<Type> SpecializedInterfaces = RandomClasses
            .Select(cls => typeof(IList<>).MakeGenericType(cls))
            .ToArray();

        private DI.ServiceContainer FContainer;

        [GlobalCleanup]
        public void Cleanup()
        {
            FContainer?.Dispose();
            FContainer = null;
        }

        [GlobalSetup(Target = nameof(Add))]
        public void SetupAdd()
        {
            FContainer = new DI.ServiceContainer();
            I = 0;
        }

        [Benchmark]
        public void Add() => FContainer.Add
        (
            new AbstractServiceEntry
            (
                typeof(IList),
                I++.ToString(),
                FContainer
            )
        );

        [GlobalSetup(Target = nameof(Get))]
        public void SetupGet() 
        {
            FContainer = new DI.ServiceContainer();
            for (int i = 0; i < InvocationCount; i++)
            {
                FContainer.Factory(typeof(IList), i.ToString(), (i, t) => null, Lifetime.Transient);
            }
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry Get() => FContainer.Get(typeof(IList), (++I % InvocationCount).ToString(), QueryModes.ThrowOnMissing);

        [GlobalSetup(Target = nameof(Specialize))]
        public void SetupSpecialize()
        {
            FContainer = new DI.ServiceContainer();
            for (int i = 0; i < InvocationCount; i++)
            {
                FContainer.Factory(typeof(IList<>), i.ToString(), (i, t) => null, Lifetime.Transient);
            }
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry Specialize()
        {
            I++;
            return FContainer.Get(SpecializedInterfaces[I % SpecializedInterfaces.Count], (I % InvocationCount).ToString(), QueryModes.AllowSpecialization | QueryModes.ThrowOnMissing);
        }

        private IServiceContainer FChild;

        [GlobalSetup(Target = nameof(SpecializeInChildContainer))]
        public void SetupSpecializeInChildContainer()
        {
            FContainer = new DI.ServiceContainer();
            for (int i = 0; i < InvocationCount; i++)
            {
                FContainer.Factory(typeof(IList<>), i.ToString(), (i, t) => null, Lifetime.Singleton);
            }
            FChild = FContainer.CreateChild();
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry SpecializeInChildContainer()
        {
            I++;
            return FChild.Get(SpecializedInterfaces[I % SpecializedInterfaces.Count], (I % InvocationCount).ToString(), QueryModes.AllowSpecialization | QueryModes.ThrowOnMissing);
        }
    }
}
