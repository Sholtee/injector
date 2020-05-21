/********************************************************************************
* Instance.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;
    using Properties;
    
    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_Instance_ShouldBeNullChecked() 
        {
            Assert.Throws<ArgumentNullException>(() => IServiceContainerExtensions.Instance(null, typeof(IDisposable), new Disposable()));
            //Assert.Throws<ArgumentNullException>(() => Container.Instance(null, new Disposable()));
            Assert.Throws<ArgumentNullException>(() => Container.Instance<IDisposable>(null));
        }

        [Test]
        public void Container_Instance_ShouldNotBeAServiceOrFactory()
        {
            Container.Instance<IInterface_1>(new Implementation_1_No_Dep());

            AbstractServiceEntry entry = Container.Get<IInterface_1>(QueryModes.ThrowOnError);

            Assert.That(entry.IsInstance());
            Assert.False(entry.IsService());
            Assert.False(entry.IsFactory());
        }

        [Test]
        public void Container_Instance_ShouldNotChange()
        {
            IInterface_1 instance = new Implementation_1_No_Dep();

            Container.Instance(instance);

            Assert.AreSame(instance, Container.CreateChild().Get<IInterface_1>(QueryModes.ThrowOnError).Instance.Value);
        }

        [Test]
        public void Container_Instance_ShouldBeTypeChecked()
        {
            Assert.Throws<InvalidOperationException>(() => Container.Instance(typeof(IInterface_1), new object()), string.Format(Resources.INTERFACE_NOT_SUPPORTED, typeof(IInterface_1)));
        }

        [Test]
        public void Container_Instance_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Container.Instance<Object>(new object()), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Container.Instance(typeof(Object), new object()), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
        }

        [Test]
        public void Container_Instance_ShouldThrowOnMultipleRegistration()
        {
            Container.Instance<IInterface_1>(new Implementation_1_No_Dep());

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Instance<IInterface_1>(new Implementation_1_No_Dep()));
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
