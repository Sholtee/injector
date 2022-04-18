/********************************************************************************
* Factory.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Factory_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionBasicExtensions.Factory(null, typeof(IDisposable), (i, t) => new Disposable(), Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Collection.Factory(null, (i, t) => new Disposable(), Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Collection.Factory(typeof(IDisposable), null, Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Collection.Factory(typeof(IDisposable), (i, t) => new Disposable(), null));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Factory_ShouldHandleGenericTypes(Lifetime lifetime)
        {
            Func<IInjector, Type, object> factory = (injector, type) => null;

            Assert.DoesNotThrow(() => Collection.Factory(typeof(IInterface_3<>), factory, lifetime));

            AbstractServiceEntry expected = lifetime.CreateFrom(typeof(IInterface_3<int>), null, factory).Last();

            Assert.That(((ISupportsSpecialization) Collection.LastEntry).Specialize(typeof(int)), Is.InstanceOf(expected.GetType()).And.EqualTo(expected).Using(ServiceIdComparer.Instance));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void Factory_ShouldThrowOnMultipleRegistration(string name)
        {
            Collection.Factory<IInterface_1>(name, me => new Implementation_1_No_Dep(), Lifetime.Transient);
            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Factory<IInterface_1>(name, me => new Implementation_1_No_Dep(), Lifetime.Transient));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Factory_ShouldAcceptNamedServices(Lifetime lifetime)
        {
            Func<IInjector, Type, IInterface_1> factory = (injector, type) => new Implementation_1_No_Dep();

            Assert.DoesNotThrow(() => Collection
                .Factory(typeof(IInterface_1), "svc1", factory, lifetime)
                .Factory(typeof(IInterface_1), "svc2", factory, lifetime));
        }
    }
}
