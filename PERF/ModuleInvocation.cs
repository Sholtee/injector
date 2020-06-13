/********************************************************************************
* ModuleInvocation.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using static Consts;

    using Interfaces;
    using Extensions;

    [MemoryDiagnoser]
    public class ModuleInvocation
    {
        public interface IModule 
        {
            int Add(int a, int b);
        }

        public class Module : IModule 
        {
            public int Add(int a, int b) => a + b;
        }

        private IServiceContainer Container { get; set; }

        private Extensions.ModuleInvocation Invoke { get; set; }

        [GlobalSetup]
        public void GlobalSetup() 
        {
            Container = new DI.ServiceContainer();
            Container.Service<IModule, Module>(Lifetime.Scoped);

            Invoke = new ModuleInvocationBuilder().Build(typeof(IModule));
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void DirectInvocation()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    int sum = injector.Get<IModule>().Add(1, 1);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void UsingTheBuiltDelegate() 
        {
            using (IInjector injector = Container.CreateInjector())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    object sum = Invoke(injector, nameof(IModule), nameof(IModule.Add), 1, 1);
                }
            }
        }
    }
}
