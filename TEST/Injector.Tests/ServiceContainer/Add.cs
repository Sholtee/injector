/********************************************************************************
* Add.cs                                                                        *
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

    public abstract partial class ServiceContainerTestsBase<TImplementation>
    {
        [Test]
        public void IServiceContainer_Add_ShouldThrowOnNull() => Assert.Throws<ArgumentNullException>(() => Container.Add(null));

        public void IServiceContainer_Add_ShouldAcceptMoreThanOneNamedService()
        {
            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), "cica", typeof(Disposable), Container)));
            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), "kutya", typeof(Disposable), Container)));

            Assert.That(Container.Count, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Add_ShouldThrowOnAlreadyRegisteredService(string name)
        {
            Container.Add(new SingletonServiceEntry(typeof(IDisposable), name, typeof(Disposable), Container));

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), name, typeof(Disposable), Container)));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Add_ShouldOverwriteAbstractEntries(string name) 
        {
            Container.Add(new AbstractServiceEntry(typeof(IDisposable), name, Container));

            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), name, typeof(Disposable), Container)));
            Assert.That(Container.Get(typeof(IDisposable), name), Is.InstanceOf<SingletonServiceEntry>());
        }

        [Test]
        public void IServiceContainer_Add_ShouldDisposeAbstractEntryOnOverride()
        {
            var entry = new AbstractServiceEntry(typeof(IInterface_1), null, Container);

            Container.Add(entry);
            Container.Add(new InstanceServiceEntry(typeof(IInterface_1), null, new Implementation_1_No_Dep(), false, Container));

            Assert.That(entry.Disposed);
        }
    }
}
