/********************************************************************************
* ServiceRegistry.cs                                                            *
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
    using Interfaces;
    using Internals;

    [MemoryDiagnoser]
    public class ServiceRegistry
    {
        public sealed class Named<T>
        {
            public string Name { get; init; }

            public T Value { get; init; }

            public override string ToString() => Name;
        }

        //
        // Mind az deklaralo osztalynak mind a ParameterSource-al annotalt property-nek publikusnak kell lennie... Viszont a ResolverBuilder
        // internal, ezert object-et adunk vissza.
        //

        public IEnumerable<Named<object>> ResolverBuilders
        {
            get
            {
                yield return new Named<object> { Name = "Chained", Value = Internals.ResolverBuilder.ChainedDelegates };
                yield return new Named<object> { Name = "Expression", Value = Internals.ResolverBuilder.CompiledExpression };
                yield return new Named<object> { Name = "Compiled", Value = Internals.ResolverBuilder.CompiledCode };
            }
        }

        [ParamsSource(nameof(ResolverBuilders))]
        public Named<object> ResolverBuilder { get; set; }

        public IEnumerable<Named<Type>> RegistryTypes
        {
            get
            {
                yield return new Named<Type> { Name = "Linear", Value = typeof(Internals.ServiceRegistry) } ;
                yield return new Named<Type> { Name = "Concurrent", Value = typeof(Internals.ConcurrentServiceRegistry) };
            }
        }

        [ParamsSource(nameof(RegistryTypes))]
        public Named<Type> RegistryType {get; set;}

        [Params(1, 10, 20, 100, 1000)]
        public int ServiceCount { get; set; }

        private ServiceRegistryBase Registry { get; set; }

        private int I { get; set; }

        [GlobalCleanup]
        public void Cleanup() => Registry.Dispose();

        [GlobalSetup(Target = nameof(GetEntry))]
        public void SetupGet()
        {
            Registry = (ServiceRegistryBase) System.Activator.CreateInstance(RegistryType.Value, new object[] 
            {
                Enumerable.Repeat(0, ServiceCount).Select((_, i) => (AbstractServiceEntry) new TransientServiceEntry(typeof(IList), i.ToString(), (i, t) => null!,  null, int.MaxValue)),
                (ResolverBuilder) ResolverBuilder.Value,
                int.MaxValue
            });
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry GetEntry() => Registry!.GetEntry(typeof(IList), (++I % ServiceCount).ToString())!;

        [GlobalSetup(Target = nameof(Specialize))]
        public void SetupSpecialize()
        {
            Registry = (ServiceRegistryBase) System.Activator.CreateInstance(RegistryType.Value, new object[]
            {
                Enumerable.Repeat(0, ServiceCount).Select((_, i) => (AbstractServiceEntry) new TransientServiceEntry(typeof(IList<>), i.ToString(), (i, t) => null!,  null, int.MaxValue)),
                (ResolverBuilder) ResolverBuilder.Value,
                int.MaxValue
            });
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry Specialize() => Registry!.GetEntry(typeof(IList<object>), (++I % ServiceCount).ToString())!;
    }
}
