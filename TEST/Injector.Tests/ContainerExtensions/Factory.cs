/********************************************************************************
* Factory.cs                                                                    *
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

    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_Factory_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceContainerBasicExtensions.Factory(null, typeof(IDisposable), (i, t) => new Disposable(), Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Container.Factory(null, (i, t) => new Disposable(), Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Container.Factory(typeof(IDisposable), null, Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Container.Factory(typeof(IDisposable), (i, t) => new Disposable(), null));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Factory_ShouldHandleGenericTypes(Lifetime lifetime)
        {
            Func<IInjector, Type, object> factory = (injector, type) => null;

            Container.Factory(typeof(IInterface_3<>), factory, lifetime);

            Assert.That(Container.Count, Is.EqualTo(1));
            Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_3<int>), null, factory, Container), Container.Get<IInterface_3<int>>(QueryModes.AllowSpecialization));
            Assert.That(Container.Count, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void Container_Factory_ShouldThrowOnMultipleRegistration(string name)
        {
            Container.Factory<IInterface_1>(name, me => new Implementation_1_No_Dep(), Lifetime.Transient);
            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Factory<IInterface_1>(name, me => new Implementation_1_No_Dep(), Lifetime.Transient));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Factory_ShouldHandleNamedServices(Lifetime lifetime)
        {
            Func<IInjector, Type, IInterface_1> factory = (injector, type) => new Implementation_1_No_Dep();

            Assert.DoesNotThrow(() => Container
                .Factory(typeof(IInterface_1), "svc1", factory, lifetime)
                .Factory(typeof(IInterface_1), "svc2", factory, lifetime));

            Assert.IsNull(Container.Get<IInterface_1>());
            Assert.IsNull(Container.Get<IInterface_1>("invalidname"));

            Assert.AreEqual(lifetime.CreateFrom(typeof(IInterface_1), "svc1", factory, Container), Container.Get<IInterface_1>("svc1"));
            Assert.AreEqual(lifetime.CreateFrom(typeof(IInterface_1), "svc2", factory, Container), Container.Get<IInterface_1>("svc2"));
        }
    }
}
