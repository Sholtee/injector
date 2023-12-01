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
            AbstractServiceEntry entry = new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default);
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
            AbstractServiceEntry entry = new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default);
            Collection.Add(entry);

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Add(entry));
            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Add(new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default)));
            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Add(new SingletonServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default)));
        }

        [Test]
        public void Add_ShouldOverrideServiceRegistration()
        {
            Collection = new ServiceCollection(supportsOverride: true);

            AbstractServiceEntry entry1 = new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default);
            Collection.Add(entry1);

            AbstractServiceEntry entry2 = new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default);
            Assert.DoesNotThrow(() => Collection.Add(entry2));
            Assert.That(Collection.Find<IMyService>(), Is.SameAs(entry2));
        }

        [Test]
        public void Clear_ShouldEmptyTheCollection()
        {
            Collection.Add(new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default));
            Collection.Clear();

            Assert.That(Collection.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveById_ShouldRemove()
        {
            Collection.Add(new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default));
            Assert.That(Collection.Remove(new ServiceId(typeof(IMyService), null)));
            Assert.That(Collection, Is.Empty);
        }

        [Test]
        public void RemoveByReference_ShouldRemove()
        {
            TransientServiceEntry entry = new(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default);
            Collection.Add(entry);

            Assert.False(Collection.Remove(item: new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default)));
            Assert.That(Collection, Is.Not.Empty);

            Assert.That(Collection.Remove(item: entry));
            Assert.That(Collection, Is.Empty);
        }

        [Test]
        public void ContainsById_ShouldSearch()
        {
            Collection.Add(new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default));
            Assert.That(Collection.Contains(new ServiceId(typeof(IMyService), null)));
        }

        [Test]
        public void ContainsByReference_ShouldSearch()
        {
            TransientServiceEntry entry = new(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default);
            Collection.Add(entry);

            Assert.False(Collection.Contains(item: new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default)));
            Assert.That(Collection.Contains(item: entry));
        }

        [Test]
        public void TryFind_ShouldReturnNullOnMissingEntry()
        {
            Assert.IsNull(Collection.TryFind(new ServiceId(typeof(IMyService), null)));
        }

        [Test]
        public void TryFind_ShouldSearch()
        {
            TransientServiceEntry entry = new(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default);
            Collection.Add(entry);

            Assert.That(Collection.TryFind(new ServiceId(typeof(IMyService), null)), Is.SameAs(entry));
        }

        [Test]
        public void MakeReadOnly_ShouldFreezeTheCollection()
        {
            Collection.Add(new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default));
            Collection.MakeReadOnly();

            Assert.Throws<InvalidOperationException>(() => Collection.Add(new TransientServiceEntry(typeof(IMyService), "cica", typeof(MyService), ServiceOptions.Default)));
            Assert.Throws<InvalidOperationException>(() => Collection.Remove(item: new TransientServiceEntry(typeof(IMyService), "cica", typeof(MyService), ServiceOptions.Default)));
            Assert.Throws<InvalidOperationException>(() => Collection.Remove(id: new ServiceId(typeof(IMyService), "cica")));
            Assert.Throws<InvalidOperationException>(() => Collection.Clear());
        }
    }
}
