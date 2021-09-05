/********************************************************************************
* Parallel.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;
    using Internals;

    using ScopeFactory = DI.ScopeFactory;

    [TestFixture]
    public partial class InjectorTests
    {
        private const int TASK_COUNT = 10;

        private static readonly IReadOnlyList<Type> RandomTypes = typeof(System.Xml.NameTable)
            .Assembly
            .GetTypes()
            .Where(t => t.IsPublic && !t.IsGenericTypeDefinition)
            .ToArray();

        [Test]
        public void Parallelism_DependencyResolutionTest(
            [ValueSource(nameof(Lifetimes))] Lifetime l1,
            [ValueSource(nameof(Lifetimes))] Lifetime l2,
            [ValueSource(nameof(Lifetimes))] Lifetime l3) 
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service(typeof(IList<>), typeof(MyList<>), l1)
                .Service<IInterface_7<IList<object>>, Implementation_7_TInterface_Dependant<IList<object>>>(l2)
                .Service<IInterface_7<IInterface_7<IList<object>>>, Implementation_7_TInterface_Dependant<IInterface_7<IList<object>>>>(l3));

            Assert.DoesNotThrow(() => Task.WaitAll
            (
                Enumerable.Repeat(0, TASK_COUNT).Select(_ => Task.Run(Worker)).ToArray()
            ));

            void Worker() 
            {
                using (IInjector injector = Root.CreateScope())
                {
                    for (int i = 0; i < 50; i++)
                        injector.Get<IInterface_7<IInterface_7<IList<object>>>>();
                }
            }
        }

        [Test]
        public void Parallelism_SpecializationTest([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service(typeof(IList<>), typeof(MyList<>), lifetime));

            Assert.DoesNotThrow(() => Task.WaitAll
            (
                Enumerable.Repeat(0, TASK_COUNT).Select(_ => Task.Run(Worker)).ToArray()
            ));

            void Worker()
            {
                using (IInjector injector = Root.CreateScope())
                {
                    foreach (Type type in RandomTypes.Take(20))
                        injector.Get(typeof(IList<>).MakeGenericType(type));
                }
            }
        }
    }

    //
    // NE InjectorTestsBase<TContainer> alatt legyen mert akkor valojaban ket generikus parametere 
    // lenne (TContainer es T).
    //

    public class MyList<T> : List<T>
    {
        public MyList() { } // h egy konstruktor legyen
    }
}
