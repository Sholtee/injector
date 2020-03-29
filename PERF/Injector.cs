/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using static Consts;

    [MemoryDiagnoser]
    public class Injector
    {
        private IServiceContainer FContainer;

        #region Services
        public interface IInterface_1
        {
        }

        public class Implementation_1 : IInterface_1
        {
        }

        public interface IInterface_2<T>
        {
            IInterface_1 Dep { get; }
        }

        public class Implementation_2<T> : IInterface_2<T>
        {
            public Implementation_2(IInterface_1 dep) => Dep = dep;

            public IInterface_1 Dep { get; }
        }

        public interface IInterface_3_LazyDep
        {
            Lazy<IInterface_1> LazyDep { get; }
        }

        public class Implementation_3_LazyDep : IInterface_3_LazyDep
        {
            public Implementation_3_LazyDep(Lazy<IInterface_1> dep) => LazyDep = dep;

            public Lazy<IInterface_1> LazyDep { get; }
        }
        #endregion

        [Params(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)]
        public Lifetime Lifetime { get; set; }

        [GlobalSetup]
        public void Setup() => FContainer = new DI.ServiceContainer()
            .Service<IInterface_1, Implementation_1>(Lifetime)
            .Service(typeof(IInterface_2<>), typeof(Implementation_2<>), Lifetime)
            .Service<IInterface_3_LazyDep, Implementation_3_LazyDep>(Lifetime);

        [GlobalCleanup]
        public void Cleanup() => FContainer.Dispose();

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Get()
        {
            using (var injector = new DI.Internals.Injector(FContainer))
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Get<IInterface_2<string>>(null);
                }
                injector.UnsafeClear();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Instantiate()
        {
            using (var injector = new DI.Internals.Injector(FContainer))
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Instantiate<Implementation_2<string>>();
                }
                injector.UnsafeClear();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Get_Lazy()
        {
            using (var injector = new DI.Internals.Injector(FContainer))
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    injector.Get<IInterface_3_LazyDep>(null);
                }
                injector.UnsafeClear();
            }
        }
    }
}
