/********************************************************************************
* InjectorLifetime.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using static Consts;

    [MemoryDiagnoser]
    public class InjectorLifetime
    {
        private static readonly IReadOnlyList<Type> RandomInterfaces = typeof(object)
            .Assembly
            .GetTypes()
            .Where(t => t.IsInterface)
            .ToArray();

        [Params(0, 10, 100)]
        public int ServiceCount { get; set; }

        private IServiceContainer FContainer;

        private class UnsafeInjector : Internals.Injector
        {
            public UnsafeInjector(IServiceContainer owner) : base(owner) { }

            public override IServiceContainer Add(AbstractServiceEntry entry)
            {
                if (entry.Owner == this)
                {
                    GC.SuppressFinalize(entry);
                    if (entry.IsInstance())
                        GC.SuppressFinalize(entry.Instance);
                }

                return base.Add(entry);
            }
        }

        [GlobalSetup]
        public void Setup() 
        {
            FContainer = new DI.ServiceContainer();

            for (int i = 0; i < ServiceCount; i++)
                FContainer.Factory(RandomInterfaces[i % RandomInterfaces.Count], (i / RandomInterfaces.Count).ToString(), (i, t) => null, (Lifetime) (i % (int) Lifetime.Singleton));
        }

        [GlobalCleanup]
        public void Cleanup() => FContainer.Dispose();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void CreateInjector() 
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var injector = new UnsafeInjector(FContainer);
                GC.SuppressFinalize(injector);
            }
            FContainer.Children.Clear();
        }
    }
}
