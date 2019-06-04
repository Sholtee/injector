/********************************************************************************
* Factory.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void Injector_Factory_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Injector.Factory<Object>(p => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Injector.Factory(typeof(Object), (p1, p2) => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }

        [Test]
        public void Injector_Factory_ShouldHandleGenericTypes()
        {
            int callCount = 0;

            Injector.Factory(typeof(IInterface_3<>), (injector, type) =>
            {
                Assert.AreSame(injector, Injector);
                Assert.AreSame(type, typeof(IInterface_3<string>));

                callCount++;
                return new Implementation_3<string>(null);
            });

            var instance = Injector.Get<IInterface_3<string>>();

            Assert.That(instance, Is.InstanceOf<Implementation_3<string>>());
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Injector_Factory_ShouldBeTypeChecked()
        {
            Injector.Factory(typeof(IInterface_1), (injector, type) => new object());

            Assert.Throws<Exception>(() => Injector.Get<IInterface_1>(), string.Format(Resources.INVALID_INSTANCE, typeof(IInterface_1)));
        }

        [Test]
        public void Injector_Factory_ShouldThrowOnMultipleRegistration()
        {
            Injector.Factory<IInterface_1>(me => new Implementation_1());

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Injector.Factory<IInterface_1>(me => new Implementation_1()));
        }
    }
}
