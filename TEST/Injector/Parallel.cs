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
    [TestFixture]
    public partial class InjectorTestsBase<TContainer>
    {
        private const int TASK_COUNT = 10;

        private static readonly IReadOnlyList<Type> RandomTypes = typeof(System.Xml.NameTable)
            .Assembly
            .GetTypes()
            .Where(t => t.IsPublic && !t.IsGenericTypeDefinition)
            .ToArray();

        [Test]
        public void Parallelism_DependencyResolutionTest(
            [Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime l1,
            [Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime l2,
            [Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime l3,
            [Values(true, false)] bool createChildContainer) 
        {
            Container
                .Service(typeof(IList<>), typeof(MyList<>), l1)
                .Service<IInterface_7<IList<object>>, Implementation_7_TInterface_Dependant<IList<object>>>(l2)
                .Service<IInterface_7<IInterface_7<IList<object>>>, Implementation_7_TInterface_Dependant<IInterface_7<IList<object>>>>(l3);

            IServiceContainer container = createChildContainer ? Container.CreateChild() : Container;

            Assert.DoesNotThrow(() => Task.WaitAll
            (
                Enumerable.Repeat(0, TASK_COUNT).Select(_ => Task.Run(Worker)).ToArray()
            ));

            void Worker() 
            {
                using (IInjector injector = container.CreateInjector())
                {
                    for (int i = 0; i < 50; i++)
                        injector.Get<IInterface_7<IInterface_7<IList<object>>>>();
                }
            }
        }

        [Test]
        public void Parallelism_SpecializationTest(
            [Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime,
            [Values(true, false)] bool createChildContainer)
        {
            Container.Service(typeof(IList<>), typeof(MyList<>), lifetime);

            IServiceContainer container = createChildContainer ? Container.CreateChild() : Container;

            Assert.DoesNotThrow(() => Task.WaitAll
            (
                Enumerable.Repeat(0, TASK_COUNT).Select(_ => Task.Run(Worker)).ToArray()
            ));

            void Worker()
            {
                using (IInjector injector = container.CreateInjector())
                {
                    foreach (Type type in RandomTypes)
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
