/********************************************************************************
* ServiceEntry.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;
using Moq;

namespace Solti.Utils.DI.Perf
{
    using static Consts;
    using Internals;

    [MemoryDiagnoser]
    public class ServiceEntry
    {
        [Params(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)]
        public Lifetime Lifetime { get; set; }

        private IServiceContainer Owner;

        [GlobalSetup]
        public void Setup() => Owner = new Mock<IServiceContainer>(MockBehavior.Strict).Object;

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Service()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var entry = ProducibleServiceEntry.Create(Lifetime, typeof(IDisposable), i.ToString(), typeof(Disposable), Owner);
                GC.SuppressFinalize(entry);
            }
        }

        private ITypeResolver Resolver;

        [GlobalSetup(Target = nameof(Lazy))]
        public void SetupLazy()
        {
            Setup();

            Resolver = new LazyTypeResolver<IDisposable>
            (
                typeof(Disposable).Assembly.Location,
                typeof(Disposable).FullName
            );
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Lazy()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var entry = ProducibleServiceEntry.Create(Lifetime, typeof(IDisposable), i.ToString(), Resolver, Owner);
                GC.SuppressFinalize(entry);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Factory()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var entry = ProducibleServiceEntry.Create(Lifetime, typeof(IDisposable), i.ToString(), new Func<IInjector, Type, object>((i, t) => new Disposable()), Owner);
                GC.SuppressFinalize(entry);
            }
        }
    }
}
