/********************************************************************************
* Remove.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;

    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Remove_ShouldHandleGenericTypes([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [Values(null, "cica")] string name)
        {
            Collection.Service(typeof(IInterface_3<>), name, typeof(Implementation_3_IInterface_1_Dependant<>), lifetime);
            int currentCount = Collection.Count;
  
            Assert.DoesNotThrow(() => Collection.Remove(typeof(IInterface_3<>), name));
            Assert.That(Collection, Has.Count.EqualTo(currentCount - 1));
        }

        [Test]
        public void Remove_ShouldHandleNamedServices([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [Values(null, "cica")] string name)
        {
            Collection.Service<IInterface_1, Implementation_1_No_Dep>(name, lifetime);
            int currentCount = Collection.Count;

            Assert.DoesNotThrow(() => Collection.Remove<IInterface_1>(name));
            Assert.That(Collection, Has.Count.EqualTo(currentCount - 1));
        }

        [Test]
        public void Remove_ShouldThrowOnMissingServce() => Assert.Throws<ServiceNotFoundException>(() => Collection.Remove<IInterface_1>());

        [Test]
        public void Remove_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionBasicExtensions.Remove<IInterface_1>(null));
            Assert.Throws<ArgumentNullException>(() => Collection.Remove(iface: null));
        }
    }
}
