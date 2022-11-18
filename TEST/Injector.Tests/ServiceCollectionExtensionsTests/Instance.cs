/********************************************************************************
* Instance.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Primitives.Patterns;
    using Properties;
    
    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Instance_ShouldBeNullChecked() 
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionAdvancedExtensions.Instance(null, typeof(IDisposable), new Disposable()));
            Assert.Throws<ArgumentNullException>(() => Collection.Instance<IDisposable>(null));
        }

        [Test]
        public void Instance_ShouldNotBeAServiceOrFactory()
        {
            Collection.Instance<IInterface_1>(new Implementation_1_No_Dep());

            AbstractServiceEntry entry = Collection.LastEntry;

            Assert.That(entry.IsInstance());
            Assert.False(entry.IsService());
            Assert.False(entry.IsFactory());
        }

        [Test]
        public void Instance_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Collection.Instance<Object>(new object()), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Collection.Instance(typeof(Object), new object()), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
        }

        [Test]
        public void Instance_ShouldThrowOnMultipleRegistration()
        {
            Collection.Instance<IInterface_1>(new Implementation_1_No_Dep());

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Instance<IInterface_1>(new Implementation_1_No_Dep()));
        }

        [Test]
        public void Instance_ShouldHandleNamedServices() 
        {
            Collection.Instance<IDisposable>(new Disposable());

            IDisposable inst = new Disposable();
            Assert.DoesNotThrow(() => Collection.Instance("cica", inst));
        }
    }
}
