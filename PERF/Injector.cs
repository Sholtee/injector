/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Annotations;
    using Internals;

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

        [Params(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)]
        public Lifetime LifeTime { get; set; }

        [GlobalSetup]
        public void Setup() => FContainer = new ServiceContainer()
            .Service<IInterface_1, Implementation_1>(LifeTime)
            .Service(typeof(IInterface_2<>), typeof(Implementation_2<>), LifeTime)
            .Service<IInterface_3<string>, Implementation_3<string>>(LifeTime);
        
        [GlobalCleanup]
        public void Cleanup() => FContainer.Dispose();

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoInjector()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                IInterface_3<string> instance = new Implementation_3<string>(new Implementation_2<string>(new Implementation_1()));
                instance.DoSomething();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Injector_Get()
        {
            using (IInjector injector = FContainer.CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    IInterface_3<string> instance = injector.Get<IInterface_3<string>>();
                    instance.DoSomething();
                }
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Injector_Instantiate()
        {
            using (IInjector injector = FContainer.CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    Implementation_3<string> instance = injector.Instantiate<Implementation_3<string>>();
                    instance.DoSomething();
                }
            }
        }
    }

    [MemoryDiagnoser]
    public class LazyInjector
    {
        public interface IInterface
        {
            void Bar();
        }

        public class Implementation : IInterface
        {            
            [ServiceActivator]
            public Implementation(Lazy<IDisposable> disposable, Lazy<IList<int>> lst)
            {               
            }

            public void Bar()
            {
            }
        }

        private IServiceContainer FContainer;

        [GlobalSetup]
        public void Setup() => FContainer = new ServiceContainer()
            .Service<IDisposable, Disposable>()
            .Factory(typeof(IList<>), (i, t) => typeof(List<>).MakeGenericType(t.GetGenericArguments()).CreateInstance(new Type[0]))
            .Service<IInterface, Implementation>();

        [GlobalCleanup]
        public void Cleanup() => FContainer.Dispose();

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoInjector()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var instance = new Implementation(new Lazy<IDisposable>(), new Lazy<IList<int>>());
                instance.Bar();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Injector_Get()
        {
            using (IInjector injector = FContainer.CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    var instance = injector.Get<IInterface>();
                    instance.Bar();
                }
            }
        }
    }
}
