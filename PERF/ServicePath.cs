/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Proxy;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 3000)]
    public class ServicePath
    {
        private Internals.ServicePath Path { get; set; }

        private readonly IInjector DummyInjector = ProxyFactory.Create<IInjector, InterfaceInterceptor<IInjector>>(new object[] { null });

        [Params(1, 5, 10)]
        public int Depth { get; set; }

        [Params(true, false)]
        public bool WithCircularityCheck { get; set; }

        [GlobalSetup(Target = nameof(Extend))]
        public void SetupExtend()
        {
            Path = new Internals.ServicePath();
        }

        [Benchmark]
        public void Extend()
        {
            Extend(0);

            void Extend(int i)
            {
                Path.Push(new ServiceReference(new DummyServiceEntry(typeof(IInjector), i.ToString()), DummyInjector));
                try
                {
                    if (WithCircularityCheck)
                        Path.CheckNotCircular();

                    if (i < Depth - 1)
                        Extend(i++);
                }
                finally 
                {
                    Path.Pop();
                }
            }
        }
    }
}
