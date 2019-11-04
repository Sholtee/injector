/********************************************************************************
* Instance.cs                                                                   *
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
        public void Container_Instance_ShouldNotBeAServiceOrFactory()
        {
            Container.Instance<IInterface_1>(new Implementation_1());

            AbstractServiceEntry entry = Container.Get<IInterface_1>(QueryMode.ThrowOnError);

            Assert.That(entry.IsInstance());
            Assert.False(entry.IsLazy());
            Assert.False(entry.IsService());
            Assert.False(entry.IsFactory());
        }

        [Test]
        public void Container_Instance_ShouldNotChange()
        {
            IInterface_1 instance = new Implementation_1();

            Container.Instance(instance);

            Assert.AreSame(instance, Container.CreateChild().Get<IInterface_1>(QueryMode.ThrowOnError).Value);
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

        [Test]
        public void Container_Instance_ShouldHandleNamedServices() 
        {
            Container.Instance<IDisposable>(new Disposable());

            IDisposable inst = new Disposable();
            Assert.DoesNotThrow(() => Container.Instance("cica", inst));
            Assert.That(Container.Get<IDisposable>("cica"), Is.EqualTo(new InstanceServiceEntry(typeof(IDisposable), "cica", inst, false, Container)));
        }
    }
}
