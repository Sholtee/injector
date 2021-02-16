/********************************************************************************
* ServiceEntry.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using static Consts;

    using Interfaces;
    using Primitives.Patterns;
    using Proxy;   

    [MemoryDiagnoser]
    public class ServiceEntry
    {
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

            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
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
