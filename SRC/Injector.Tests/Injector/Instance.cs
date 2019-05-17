﻿/********************************************************************************
* Instance.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void Injector_Instance_ShouldNotChange()
        {
            IInterface_1 instance = new Implementation_1();

            Injector.Instance(instance);

            Assert.AreSame(instance, Injector.Get<IInterface_1>());
        }

        [Test]
        public void Injector_Instance_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Injector.Instance<Object>(null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Injector.Instance(typeof(Object), null), string.Format(Resources.NOT_AN_INTERFACE, "iface"));
        }
    }
}
