/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;

    [TestFixture]
    public sealed class ServiceCollectionTests
    {
        private interface IMyService { }

        private class MyService : IMyService { }

        public IServiceCollection Collection { get; set; }

        [SetUp]
        public void Setup() => Collection = new ServiceCollection();

        [Test]
        public void Add_ShouldExpandTheCollection()
        {
            AbstractServiceEntry entry = new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), Collection.ServiceOptions);
            Collection.Add(entry);

            Assert.That(Collection.Count, Is.EqualTo(1));
            Assert.That(Collection.Single(), Is.SameAs(entry));
        }

        [Test]
        public void Add_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => Collection.Add(null));
        }

        [Test]
        public void Add_ShouldThrowOnDuplicateServiceRegistration()
        {
            AbstractServiceEntry entry = new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), Collection.ServiceOptions);
            Collection.Add(entry);

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Add(entry));
            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Add(new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), Collection.ServiceOptions)));
            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Add(new SingletonServiceEntry(typeof(IMyService), null, typeof(MyService), Collection.ServiceOptions)));
        }

        [Test]
        public void Clear_ShouldEmptyTheCollection()
        {
            Collection.Add(new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), Collection.ServiceOptions));
            Collection.Clear();

            Assert.That(Collection.Count, Is.EqualTo(0));
        }
    }
}
