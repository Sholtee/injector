/********************************************************************************
* Service.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class ContainerTests
    {
        [Test]
        public void Container_Service_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Container.Service<Object, Object>(), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Container.Service(typeof(Object), typeof(Object)), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Singleton)]
        public void Container_Service_ShouldHandleGenericTypes(Lifetime lifetime)
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Service(typeof(IInterface_3<>), typeof(Implementation_3<>), lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_3<int>>();

                Assert.That(instance, Is.InstanceOf<Implementation_3<int>>());
            }
        }

        [Test]
        public void Container_Service_ShouldHandleClosedGenericTypes()
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Service<IInterface_3<int>, Implementation_3<int>>();

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.That(injector.Get<IInterface_3<int>>(), Is.InstanceOf<Implementation_3<int>>());
            }
        }

        [Test]
        public void Container_Service_ShouldThrowOnMultipleConstructors()
        {
            Assert.Throws<NotSupportedException>(
                () => Container.Service<IList<int>, List<int>>(),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<int>)));

            //
            // Generikusnal legkorabban csak peldanyositaskor derulhet ki h szopas van.
            //

            Assert.Throws<NotSupportedException>(
                () => Container
                    .Service(typeof(IList<>), typeof(List<>))
                    .CreateInjector()
                    .Get<IList<int>>(),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<>)));
        }

        [Test]
        public void Container_Service_ShouldBeInstructedByServiceActivator()
        {
            Assert.DoesNotThrow(() => Container.Service<IInterface_1, Implementation_8_multictor>());
            Assert.DoesNotThrow(() => Container.Service(typeof(IInterface_3<>), typeof(Implementation_9_multictor<>)));

            using (IInjector injector = Container.CreateInjector())
            {
                var obj = injector.Get<IInterface_1>();
                Assert.That(obj, Is.InstanceOf<Implementation_8_multictor>());

                var obj2 = injector.Get<IInterface_3<string>>();
                Assert.That(obj2, Is.InstanceOf<Implementation_9_multictor<string>>());
                Assert.That(obj2.Interface1, Is.InstanceOf<Implementation_8_multictor>());
            }
        }

        [Test]
        public void Container_Service_ShouldThrowOnConstructorHavingNonInterfaceArgument()
        {
            Assert.Throws<ArgumentException>(() => Container.Service<IInterface_1, Implementation_1_Invalid>());
        }

        [Test]
        public void Container_Service_ShouldThrowIfTheInterfaceIsNotAssignableFromTheImplementation()
        {
            Assert.Throws<InvalidOperationException>(() => Container.Service(typeof(IInterface_2), typeof(Implementation_1)), string.Format(Resources.NOT_ASSIGNABLE, typeof(IInterface_2), typeof(Implementation_1)));
            Assert.Throws<InvalidOperationException>(() => Container.Service(typeof(IList<>), typeof(Implementation_1)), string.Format(Resources.NOT_ASSIGNABLE, typeof(IList<>), typeof(Implementation_1)));
            Assert.Throws<InvalidOperationException>(() => Container.Service(typeof(IList<int>), typeof(List<string>)), string.Format(Resources.NOT_ASSIGNABLE, typeof(IList<int>), typeof(List<string>)));
        }

        [Test]
        public void Container_Service_ShouldThrowOnMultipleRegistration()
        {
            Container.Service<IInterface_1, Implementation_1>();

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Service<IInterface_1, Implementation_1>());
        }
    }
}
