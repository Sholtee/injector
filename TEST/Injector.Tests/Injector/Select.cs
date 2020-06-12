/********************************************************************************
* Select.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;
    using Internals;

    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Injector_Select_ShouldReturnAllTheRegisteredServicesWithTheGivenInterface() 
        {
            Container
                .Service<IInterface_1, Implementation_1>(Lifetime.Transient)
                .Service<IInterface_1, Implementation_2>(1.ToString(), Lifetime.Scoped)
                .Service<IInterface_1, Implementation_3>(2.ToString(), Lifetime.Singleton)
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>();

            using (IInjector injector = Container.CreateInjector()) 
            {
                IInterface_1[] svcs = injector
                    .Select(typeof(IInterface_1))
                    .Cast<IInterface_1>()
                    .ToArray();

                Assert.That(svcs.Length, Is.EqualTo(3));
                Assert.That(svcs[0], Is.InstanceOf<Implementation_1>());
                Assert.That(svcs[1], Is.InstanceOf<Implementation_2>());
                Assert.That(svcs[2], Is.InstanceOf<Implementation_3>());
            }
        }

        [Test]
        public void Injector_Select_ShouldReturnAllTheRegisteredServicesWithTheGivenGenericInterface()
        {
            Container
                .Service(typeof(IInterface_3<>), typeof(GenericImplementation_1<>), Lifetime.Singleton)
                .Service(typeof(IInterface_3<>), 1.ToString(), typeof(GenericImplementation_2<>), Lifetime.Scoped)
                .Service(typeof(IInterface_3<>), 2.ToString(), typeof(GenericImplementation_3<>), Lifetime.Transient)
                .Service<IInterface_1, Implementation_1>(Lifetime.Singleton); // StrictDI-t ne szegjuk meg

            using (IInjector injector = Container.CreateInjector())
            {
                IInterface_3<int>[] svcs = injector
                    .Select(typeof(IInterface_3<int>))
                    .Cast<IInterface_3<int>>()
                    .ToArray();

                Assert.That(svcs.Length, Is.EqualTo(3));
                Assert.That(svcs[0], Is.InstanceOf<GenericImplementation_1<int>>());
                Assert.That(svcs[1], Is.InstanceOf<GenericImplementation_2<int>>());
                Assert.That(svcs[2], Is.InstanceOf<GenericImplementation_3<int>>());
            }
        }

        private class Implementation_1 : IInterface_1 { }
        private class Implementation_2 : IInterface_1 { }
        private class Implementation_3 : IInterface_1 { }

        private class GenericImplementation_1<T> : IInterface_3<T>
        {
            public GenericImplementation_1(IInterface_1 dep) => Interface1 = dep;
            public IInterface_1 Interface1 { get; }
        }

        private class GenericImplementation_2<T> : IInterface_3<T>
        {
            public GenericImplementation_2(IInterface_1 dep) => Interface1 = dep;
            public IInterface_1 Interface1 { get; }
        }

        private class GenericImplementation_3<T> : IInterface_3<T>
        {
            public GenericImplementation_3(IInterface_1 dep) => Interface1 = dep;
            public IInterface_1 Interface1 { get; }
        }

        [Test]
        public void Injector_Select_EnumerableMayBeEnumeratedMultipleTimes() 
        {
            Container
                .Service<IInterface_1, Implementation_1>();

            using (IInjector injector = Container.CreateInjector())
            {
                IEnumerable svcs = injector.Select(typeof(IInterface_1));

                for (int i = 0; i < 3; i++)
                {
                    Assert.DoesNotThrow(() => svcs.Cast<IInterface_1>().ToArray());
                }
            }
        }

        [Test]
        public void Injector_Select_ShouldHandleOpenAndClosedGenericsTogether()
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Service(typeof(IInterface_3<>), 1.ToString(), typeof(GenericImplementation_1<>))
                .Service(typeof(IInterface_3<>), 2.ToString(), typeof(GenericImplementation_2<>))
                .Service<IInterface_3<int>, GenericImplementation_3<int>>();

            using (IInjector injector = Container.CreateInjector())
            {
                IEnumerable<IInterface_3<int>> svcs = injector
                    .Select(typeof(IInterface_3<int>))
                    .Cast<IInterface_3<int>>();

                Assert.That(svcs.Count(), Is.EqualTo(3));
                Assert.That(svcs.Any(svc => svc is GenericImplementation_1<int>));
                Assert.That(svcs.Any(svc => svc is GenericImplementation_2<int>));
                Assert.That(svcs.Any(svc => svc is GenericImplementation_3<int>));
            }
        }

        [Test]
        public void Injector_Select_TransientServicesShouldBeUniqueInEachEnumeration() 
        {
            Container.Service<IInterface_1, Implementation_1>(Lifetime.Transient);

            using (IInjector injector = Container.CreateInjector()) 
            {
                IEnumerable<IInterface_1> svcs = injector.Get<IEnumerable<IInterface_1>>();
                Assert.AreNotSame(svcs.Single(), svcs.Single());
            }
        }

        [Test]
        public void Injector_Select_NotTransientServicesShouldBeTheSameInEachEnumeration([Values(Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime)
        {
            Container.Service<IInterface_1, Implementation_1>(lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                IEnumerable<IInterface_1> svcs = injector.Get<IEnumerable<IInterface_1>>();
                Assert.AreSame(svcs.Single(), svcs.Single());
            }
        }
    }
}
