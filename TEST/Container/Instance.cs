/********************************************************************************
* Instance.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class ContainerTests
    {
        [Test]
        public void Container_Instance_ShouldNotBeAServiceOrFactory()
        {
            Container.Instance<IInterface_1>(new Implementation_1());

            IServiceInfo serviceInfo = Container.QueryServiceInfo<IInterface_1>();

            Assert.That(serviceInfo.IsInstance());
            Assert.False(serviceInfo.IsLazy());
            Assert.False(serviceInfo.IsService());
            Assert.False(serviceInfo.IsFactory());
        }

        [Test]
        public void Container_Instance_ShouldNotChange()
        {
            IInterface_1 instance = new Implementation_1();

            Container.Instance(instance);

            Assert.AreSame(instance, Container.CreateChild().QueryServiceInfo<IInterface_1>().Value);
        }

        [Test]
        public void Container_Instance_ShouldBeTypeChecked()
        {
            Assert.Throws<InvalidOperationException>(() => Container.Instance(typeof(IInterface_1), new object()), string.Format(Resources.NOT_ASSIGNABLE, typeof(IInterface_1), typeof(object)));
        }

        [Test]
        public void Container_Instance_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Container.Instance<Object>(new object()), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Container.Instance(typeof(Object), new object()), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }

        [Test]
        public void Container_Instance_ShouldThrowOnMultipleRegistration()
        {
            Container.Instance<IInterface_1>(new Implementation_1());

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Instance<IInterface_1>(new Implementation_1()));
        }
    }
}
