/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    [MemoryDiagnoser]
    public class Injector
    {
        private const int OperationsPerInvoke = 50000;

        private IInjector injector;

        #region Services
        public interface IInterface_1
        {
        }

        public class Implementation_1 : IInterface_1
        {
        }

        public interface IInterface_2<T>
        {            
        }

        public class Implementation_2<T> : IInterface_2<T>
        {
            public Implementation_2(IInterface_1 dep)
            {               
            }
        }

        public interface IInterface_3<T>
        {          
        }

        public class Implementation_3<T> : IInterface_3<T>
        {
            public Implementation_3(IInterface_2<T> dep)
            {
            }
        }
        #endregion

        [Params(Lifetime.Transient, Lifetime.Singleton)]
        public Lifetime LifeTime { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            injector = DI.Injector.Create()
                .Service<IInterface_1, Implementation_1>(Lifetime.Transient)
                .Service(typeof(IInterface_2<>), typeof(Implementation_2<>), Lifetime.Transient)
                .Service<IInterface_3<string>, Implementation_3<string>>(LifeTime);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            injector.Dispose();
            injector = null;
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoInjector()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                IInterface_3<string> iface = new Implementation_3<string>(new Implementation_2<string>(new Implementation_1()));
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void RootInjector()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                IInterface_3<string> iface = injector.Get<IInterface_3<string>>();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void ChildInjector()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                using (IInjector child = injector.CreateChild())
                {
                    IInterface_3<string> iface = child.Get<IInterface_3<string>>();
                }  
            }
        }
    }
}
