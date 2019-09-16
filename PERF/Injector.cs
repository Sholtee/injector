/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using static Consts;

    [MemoryDiagnoser, MarkdownExporterAttribute.GitHub]
    public class Injector
    {
        private IInjector FInjector;

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
            int DoSomething();
        }

        public class Implementation_3<T> : IInterface_3<T>
        {
            public Implementation_3(IInterface_2<T> dep)
            {
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public int DoSomething() => 0;
        }
        #endregion

        [Params(Lifetime.Transient, Lifetime.Scoped)]
        public Lifetime LifeTime { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            FInjector = ServiceContainer.Create()
                .Service<IInterface_1, Implementation_1>(LifeTime)
                .Service(typeof(IInterface_2<>), typeof(Implementation_2<>), LifeTime)
                .Service<IInterface_3<string>, Implementation_3<string>>(LifeTime)
                .CreateInjector();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            FInjector.Dispose();
            FInjector = null;
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoInjector()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                IInterface_3<string> iface = new Implementation_3<string>(new Implementation_2<string>(new Implementation_1()));
                iface.DoSomething();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Injector_Get()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                IInterface_3<string> iface = FInjector.Get<IInterface_3<string>>();
                iface.DoSomething();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Injector_Instantiate()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Implementation_3<string> iface = FInjector.Instantiate<Implementation_3<string>>();
                iface.DoSomething();
            }
        }
    }
}
