/********************************************************************************
* Find.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;

    [TestFixture]
    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Find_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionBasicExtensions.Find<IInterface_1>(null));
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionBasicExtensions.Find<IInterface_1>(null, null));
            Assert.Throws<ArgumentNullException>(() => Collection.Find(null));
            Assert.Throws<ArgumentNullException>(() => Collection.Find(null, null));
        }

        [Test]
        public void TryFind_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionBasicExtensions.TryFind<IInterface_1>(null));
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionBasicExtensions.TryFind<IInterface_1>(null, null));
            Assert.Throws<ArgumentNullException>(() => Collection.TryFind(null));
            Assert.Throws<ArgumentNullException>(() => Collection.TryFind(null, null));
        }

        [Test]
        public void Find_ShouldThrowOnMissingService()
        {
            Assert.Throws<ServiceNotFoundException>(() => Collection.Find<IInterface_1>());
            Assert.Throws<ServiceNotFoundException>(() => Collection.Find<IInterface_1>(null));
        }

        [Test]
        public void TryFind_ShouldReturnNullOnMissingService()
        {
            Assert.That(Collection.TryFind<IInterface_1>(), Is.Null);
            Assert.That(Collection.TryFind<IInterface_1>(null), Is.Null);
        }

        [Test]
        public void Find_ShouldReturnTheProperService([Values(null, "name")] string name)
        {
            Collection.Service<IInterface_1, Implementation_1_No_Dep>(name, Lifetime.Transient);

            AbstractServiceEntry svc = Collection.Find<IInterface_1>(name);

            Assert.That(svc, Is.Not.Null);
            Assert.That(svc.Type, Is.EqualTo(typeof(IInterface_1)));
            Assert.That(svc.Key, Is.EqualTo(name));
        }

        [Test]
        public void TryFind_ShouldReturnTheProperService([Values(null, "name")] string name)
        {
            Collection.Service<IInterface_1, Implementation_1_No_Dep>(name, Lifetime.Transient);

            AbstractServiceEntry svc = Collection.TryFind<IInterface_1>(name);

            Assert.That(svc, Is.Not.Null);
            Assert.That(svc.Type, Is.EqualTo(typeof(IInterface_1)));
            Assert.That(svc.Key, Is.EqualTo(name));
        }
    }
}
