/********************************************************************************
* ServiceEntry.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using static Consts;

    using Interfaces;
    using Internals;
    using Primitives.Patterns;
    using Proxy;   

    [MemoryDiagnoser]
    public class ServiceEntry
    {
        static ServiceEntry() =>
            //
            // Ugy tunik a modul inicializalok nem futnak ha a kodunkat a BenchmarkDotNet forditja
            //

            InjectorDotNetLifetime.Initialize();

        public IEnumerable<Lifetime> Lifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
                yield return Lifetime.Singleton;
                yield return Lifetime.Pooled.WithCapacity(4);
            }
        }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime Lifetime { get; set; }

        private IServiceContainer Owner;

        public class DummyContainer : InterfaceInterceptor<IServiceContainer>
        {
            public DummyContainer() : base(null) {}

            public override object Invoke(InvocationContext context)
                => throw new InvalidOperationException("Owner methods should not be invoked.");
        }

        [GlobalSetup]
        public void Setup() => Owner = ProxyFactory.Create<IServiceContainer, DummyContainer>();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Service()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                foreach(AbstractServiceEntry entry in Lifetime.CreateFrom(typeof(IDisposable), i.ToString(), typeof(Disposable), Owner))
                    GC.SuppressFinalize(entry);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Factory()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                foreach(AbstractServiceEntry entry in Lifetime.CreateFrom(typeof(IDisposable), i.ToString(), new Func<IInjector, Type, object>((i, t) => new Disposable()), Owner))
                    GC.SuppressFinalize(entry);
            }
        }
    }
}
