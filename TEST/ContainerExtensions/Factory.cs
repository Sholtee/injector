/********************************************************************************
* Factory.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Internals;
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
            Func<IInjector, Type, object> factory = (injector, type) => null;

            Container.Factory(typeof(IInterface_3<>), factory);

            Assert.That(Container.Count, Is.EqualTo(1));
            Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_3<int>), null, factory, Container), Container.Get<IInterface_3<int>>(QueryMode.AllowSpecialization));
            Assert.That(Container.Count, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void Container_Factory_ShouldThrowOnMultipleRegistration(string name)
        {
            Container.Factory<IInterface_1>(name, me => new Implementation_1());
            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Factory<IInterface_1>(name, me => new Implementation_1()));
        }

        [Test]
        public void Container_Factory_ShouldHandleNamedServices()
        {
            Func<IInjector, Type, IInterface_1> factory = (injector, type) => new Implementation_1();

            Assert.DoesNotThrow(() => Container
                .Factory(typeof(IInterface_1), "svc1", factory)
                .Factory(typeof(IInterface_1), "svc2", factory));

            Assert.IsNull(Container.Get<IInterface_1>());
            Assert.IsNull(Container.Get<IInterface_1>("invalidname"));

            Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_1), "svc1", factory, Container), Container.Get<IInterface_1>("svc1"));
            Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_1), "svc2", factory, Container), Container.Get<IInterface_1>("svc2"));
        }
    }
}
