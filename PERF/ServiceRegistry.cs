/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;

    [MemoryDiagnoser]
    public class ServiceRegistry
    {
        public IEnumerable<IResolverBuilder> ResolverBuilders
        {
            get
            {
                yield return new ChainedMethodsResolverBuilder();
                yield return new CompiledExpressionResolverBuilder();
            }
        }

        [ParamsSource(nameof(ResolverBuilders))]
        public IResolverBuilder ResolverBuilder {get; set;} 

        [Params(1, 10, 20, 100, 1000)]
        public int ServiceCount { get; set; }

        private Internals.ServiceRegistry Registry { get; set; }

        private int I { get; set; }

        [GlobalCleanup]
        public void Cleanup() => Registry.Dispose();

        [GlobalSetup(Target = nameof(GetEntry))]
        public void SetupGet()
        {
            Registry = new Internals.ServiceRegistry(Enumerable.Repeat(0, ServiceCount).Select((_, i) => new TransientServiceEntry(typeof(IList), i.ToString(), (i, t) => null!, new DI.ServiceContainer(), int.MaxValue)), ResolverBuilder);
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry GetEntry() => Registry!.GetEntry(typeof(IList), (++I % ServiceCount).ToString())!;

        [GlobalSetup(Target = nameof(Specialize))]
        public void SetupSpecialize()
        {
            Registry = new Internals.ServiceRegistry(Enumerable.Repeat(0, ServiceCount).Select((_, i) => new TransientServiceEntry(typeof(IList<>), i.ToString(), (i, t) => null!, new DI.ServiceContainer(), int.MaxValue)), ResolverBuilder);
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry Specialize() => Registry!.GetEntry(typeof(IList<object>), (++I % ServiceCount).ToString())!;
    }
}
