/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;
    using Primitives;
    using Primitives.Patterns;

    public abstract class ServiceRegistryTestsBase
    {
        public sealed class Named<T>
        {
            public string Name { get; init; }
            public T Value { get; init; }
            public override string ToString() => Name;
        }

        public static IEnumerable<Named<Type>> RegistryTypes
        {
            get
            {
                yield return new Named<Type> { Name = "Linear", Value = typeof(ServiceRegistry) };
                yield return new Named<Type> { Name = "Concurrent", Value = typeof(ConcurrentServiceRegistry) };
            }
        }

        //
        // Mind az deklaralo osztalynak mind a ParameterSource-al annotalt property-nek publikusnak kell lennie... Viszont a ResolverBuilder
        // internal, ezert object-et adunk vissza.
        //

        public static IEnumerable<Named<object>> ResolverBuilders
        {
            get
            {
                yield return new Named<object> { Name = "Chained", Value = Internals.ResolverBuilder.ChainedDelegates };
                yield return new Named<object> { Name = "Expression", Value = Internals.ResolverBuilder.CompiledExpression };
                yield return new Named<object> { Name = "Compiled", Value = Internals.ResolverBuilder.CompiledCode };
            }
        }

        [ParamsSource(nameof(RegistryTypes))]
        public Named<Type> RegistryType { get; set; }
    }

    [MemoryDiagnoser]
    public class ServiceRegistry_GetEntry: ServiceRegistryTestsBase
    {
        [ParamsSource(nameof(ResolverBuilders))]
        public Named<object> ResolverBuilder { get; set; }

        [Params(1, 10, 20, 100, 1000)]
        public int ServiceCount { get; set; }

        private ServiceRegistryBase Registry { get; set; }

        private int I { get; set; }

        [GlobalCleanup]
        public void Cleanup() => Registry.Dispose();

        [GlobalSetup(Target = nameof(ResolveRegularEntry))]
        public void SetupResolveRegularEntry()
        {
            Registry = (ServiceRegistryBase) System.Activator.CreateInstance(RegistryType.Value, new object[] 
            {
                new HashSet<AbstractServiceEntry>
                (
                    Enumerable
                        .Repeat(0, ServiceCount)
                        .Select
                        (
                            (_, i) => (AbstractServiceEntry) new TransientServiceEntry(typeof(IList), i.ToString(), (i, t) => null!,  null)
                        ),
                    ServiceIdComparer.Instance
                ),
                (ResolverBuilder) ResolverBuilder.Value,
                CancellationToken.None
            });
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry ResolveRegularEntry() => Registry!.GetEntry(typeof(IList), (++I % ServiceCount).ToString())!;

        [GlobalSetup(Target = nameof(ResolveGenericEntry))]
        public void SetupResolveGenericEntry()
        {
            Registry = (ServiceRegistryBase) System.Activator.CreateInstance(RegistryType.Value, new object[]
            {
                new HashSet<AbstractServiceEntry>
                (
                    Enumerable
                        .Repeat(0, ServiceCount)
                        .Select
                        (
                            (_, i) => (AbstractServiceEntry) new TransientServiceEntry(typeof(IList<>), i.ToString(), (i, t) => null!,  null)
                        ),
                    ServiceIdComparer.Instance
                ),
                (ResolverBuilder) ResolverBuilder.Value,
                CancellationToken.None
            });
            I = 0;
        }

        [Benchmark]
        public AbstractServiceEntry ResolveGenericEntry() => Registry!.GetEntry(typeof(IList<object>), (++I % ServiceCount).ToString())!;
    }

    [MemoryDiagnoser]
    public class ServiceRegistry_Derive : ServiceRegistryTestsBase
    {
        private ServiceRegistryBase Registry { get; set; }

        private Func<object[], object> Factory { get; set; }

        [Params(0, 1, 5, 20)]
        public int EntryCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Registry = (ServiceRegistryBase) System.Activator.CreateInstance(RegistryType.Value, new object[]
            {
                new HashSet<AbstractServiceEntry>
                (
                    Enumerable.Repeat(0, EntryCount).Select((_, i) => new ScopedServiceEntry(typeof(IDisposable), i.ToString(), typeof(Disposable), null)),
                    ServiceIdComparer.Instance
                ),
                ResolverBuilder.CompiledExpression,
                CancellationToken.None
            });

            Factory = (RegistryType.Value.GetConstructor(new Type[] { RegistryType.Value }) ?? RegistryType.Value.GetConstructor(new Type[] { typeof(ServiceRegistryBase) }))
                .ToStaticDelegate();
        }

        [GlobalCleanup]
        public void Cleanup() => Registry?.Dispose();

        [Benchmark]
        public void DeriveAndDispose()
        {
            using (IServiceRegistry child = (IServiceRegistry) Factory(new object[] { Registry }))
            {
                _ = child.Parent;
            }
        }

        [Benchmark]
        public async Task DeriveAndDisposeAsync()
        {
            await using (IServiceRegistry child = (IServiceRegistry) Factory(new object[] { Registry }))
            {
                _ = child.Parent;
            }
        }
    }

    [MemoryDiagnoser]
    public class ServiceRegistry_Create : ServiceRegistryTestsBase
    {
        [ParamsSource(nameof(ResolverBuilders))]
        public Named<object> ResolverBuilder { get; set; }

        private Func<object[], object> Factory { get; set; }

        private ISet<AbstractServiceEntry> EmptySet { get; } = new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance);

        [GlobalSetup]
        public void Setup()
        {
            Factory = RegistryType
                .Value
                .GetConstructor(new Type[] { typeof(ISet<AbstractServiceEntry>), typeof(ResolverBuilder), typeof(CancellationToken) })
                .ToStaticDelegate();
        }

        [Benchmark]
        public IServiceRegistry Create() => (IServiceRegistry) Factory(new object[] { EmptySet, ResolverBuilder.Value, CancellationToken.None } );
    }
}
