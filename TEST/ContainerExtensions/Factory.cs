/********************************************************************************
* Factory.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Properties;
    
    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_Factory_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Container.Factory<Object>(p => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Container.Factory(typeof(Object), (p1, p2) => null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }

        [Test]
        public void Container_Factory_ShouldHandleGenericTypes()
        {
            int callCount = 0;

            Container.Factory(typeof(IInterface_3<>), (injector, type) =>
            {
                Assert.AreSame(type, typeof(IInterface_3<string>));

                callCount++;
                return new Implementation_3<string>(null);
            });

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_3<string>>();

                Assert.That(instance, Is.InstanceOf<Implementation_3<string>>());
                Assert.That(callCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Container_Factory_ShouldBeTypeChecked()
        {
            Container.Factory(typeof(IInterface_1), (injector, type) => new object());

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<Exception>(() => injector.Get<IInterface_1>(), string.Format(Resources.INVALID_INSTANCE, typeof(IInterface_1)));
            }            
        }

        [Test]
        public void Container_Factory_ShouldThrowOnMultipleRegistration()
        {
            Container.Factory<IInterface_1>(me => new Implementation_1());

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Factory<IInterface_1>(me => new Implementation_1()));
        }
    }
}
