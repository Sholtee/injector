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

namespace Solti.Utils.DI.Tests
{
    using Interfaces;

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
        public void Parallelism_DependencyResolutionInASeparateScope(
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
        public void Parallelism_DependencyResolutionInTheSameScope(
            [ValueSource(nameof(Lifetimes))] Lifetime l1,
            [ValueSource(nameof(Lifetimes))] Lifetime l2,
            [ValueSource(nameof(Lifetimes))] Lifetime l3)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service(typeof(IList<>), typeof(MyList<>), l1)
                .Service<IInterface_7<IList<object>>, Implementation_7_TInterface_Dependant<IList<object>>>(l2)
                .Service<IInterface_7<IInterface_7<IList<object>>>, Implementation_7_TInterface_Dependant<IInterface_7<IList<object>>>>(l3));

            IInjector injector = (IInjector) Root;

            Assert.DoesNotThrow(() => Task.WaitAll
            (
                Enumerable.Repeat(0, TASK_COUNT).Select(_ => Task.Run(Worker)).ToArray()
            ));

            void Worker()
            {
                for (int i = 0; i < 50; i++)
                    injector.Get<IInterface_7<IInterface_7<IList<object>>>>();
            }
        }

        [Test]
        public void Parallelism_SpecializationInASeparateScope([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
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

        [Test]
        public void Parallelism_SpecializationInTheSameScope([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service(typeof(IList<>), typeof(MyList<>), lifetime));

            IInjector injector = (IInjector) Root;

            Assert.DoesNotThrow(() => Task.WaitAll
            (
                Enumerable.Repeat(0, TASK_COUNT).Select(_ => Task.Run(Worker)).ToArray()
            ));

            void Worker()
            {
                foreach (Type type in RandomTypes.Take(20))
                    injector.Get(typeof(IList<>).MakeGenericType(type));
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
