/********************************************************************************
* Service.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void Injector_Service_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Injector.Service<Object, Object>(), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Injector.Service(typeof(Object), typeof(Object)), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Service_ShouldHandleGenericTypes(Lifetime lifetime)
        {
            Injector
                .Service<IInterface_1, Implementation_1>()
                .Service(typeof(IInterface_3<>), typeof(Implementation_3<>), lifetime);

            var instance = Injector.Get<IInterface_3<int>>();

            Assert.That(instance, Is.InstanceOf<Implementation_3<int>>());
            (lifetime == Lifetime.Singleton ? Assert.AreSame : ((Action<object, object>) Assert.AreNotSame))(instance, Injector.Get<IInterface_3<int>>());
        }

        [Test]
        public void Injector_Service_ShouldHandleClosedGenericTypes()
        {
            Injector
                .Service<IInterface_1, Implementation_1>()
                .Service<IInterface_3<int>, Implementation_3<int>>();

            Assert.That(Injector.Get<IInterface_3<int>>(), Is.InstanceOf<Implementation_3<int>>());
        }

        [Test]
        public void Injector_Service_ShouldThrowOnMultipleConstructors()
        {
            Assert.Throws<NotSupportedException>(
                () => Injector.Service(typeof(IList<>), typeof(List<>)),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<>)));

            Assert.Throws<NotSupportedException>(
                () => Injector.Service<IList<int>, List<int>>(),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<int>)));
        }

        [Test]
        public void Injector_Service_ShouldThrowIfTheInterfaceIsNotAssignableFromTheImplementation()
        {
            Assert.Throws<InvalidOperationException>(() => Injector.Service<IInterface_2, Implementation_1>(), string.Format(Resources.NOT_ASSIGNABLE, typeof(IInterface_2), typeof(Implementation_1)));
            Assert.Throws<InvalidOperationException>(() => Injector.Service(typeof(IList<>), typeof(Implementation_1)), string.Format(Resources.NOT_ASSIGNABLE, typeof(IList<>), typeof(Implementation_1)));
            Assert.Throws<InvalidOperationException>(() => Injector.Service<IList<int>, List<string>>(), string.Format(Resources.NOT_ASSIGNABLE, typeof(IList<int>), typeof(List<string>)));
        }
    }
}
