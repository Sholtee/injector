/********************************************************************************
* Service.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Moq;
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
        public void Container_Service_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceContainerBasicExtensions.Service(null, typeof(IDisposable), typeof(Disposable), Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Container.Service(null, typeof(Disposable), Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Container.Service(typeof(IDisposable), null, Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Container.Service(typeof(IDisposable), typeof(Disposable), null));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Service_ShouldThrowOnNonInterfaceKey(Lifetime lifetime)
        {
            Assert.Throws<ArgumentException>(() => Container.Service<Object, Object>(lifetime), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Container.Service(typeof(Object), typeof(Object), lifetime), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Service_ShouldHandleGenericTypes(Lifetime lifetime)
        {
            Container
                .Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), lifetime);

            AbstractServiceEntry entry = Container.Get<IInterface_3<int>>(QueryModes.AllowSpecialization);

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Interface, Is.EqualTo(typeof(IInterface_3<int>)));
            Assert.That(entry.Implementation, Is.EqualTo(typeof(Implementation_3_IInterface_1_Dependant<int>)));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Service_ShouldHandleNamedServices(Lifetime lifetime) 
        {
            Assert.DoesNotThrow(() => Container
                .Service<IInterface_1, Implementation_1_No_Dep>("svc1", lifetime)
                .Service<IInterface_1, DecoratedImplementation_1>("svc2", lifetime));

            Assert.IsNull(Container.Get<IInterface_1>());
            Assert.IsNull(Container.Get<IInterface_1>("invalidname"));

            Assert.AreEqual(lifetime.CreateFrom(typeof(IInterface_1), "svc1", typeof(Implementation_1_No_Dep), Container).Where(entry => !entry.IsInternal()).Single(), Container.Get<IInterface_1>("svc1"));
            Assert.AreEqual(lifetime.CreateFrom(typeof(IInterface_1), "svc2", typeof(DecoratedImplementation_1), Container).Where(entry => !entry.IsInternal()).Single(), Container.Get<IInterface_1>("svc2"));
        }

        private interface IServiceHavingNonInterfaceCtorArg
        {
            string CtorParam { get; }
        }

        private class ServiceHavingNonInterfaceCtorArg: IServiceHavingNonInterfaceCtorArg
        {
            public ServiceHavingNonInterfaceCtorArg(string para, IInterface_1 unused) => CtorParam = para;

            public string CtorParam { get; }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Service_ShouldHandleExplicitArgs(Lifetime lifetime)
        {
            Container.Service<IServiceHavingNonInterfaceCtorArg, ServiceHavingNonInterfaceCtorArg>(new Dictionary<string, object> { ["para"] = "cica" }, lifetime);

            Func<IInjector, Type, object> fact = Container.Get<IServiceHavingNonInterfaceCtorArg>().Factory;

            Assert.That(fact, Is.Not.Null);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IInterface_1), null))
                .Returns(null);

            IServiceHavingNonInterfaceCtorArg myService = (IServiceHavingNonInterfaceCtorArg) fact(mockInjector.Object, typeof(IServiceHavingNonInterfaceCtorArg));

            Assert.That(myService.CtorParam, Is.EqualTo("cica"));

            mockInjector.Verify(i => i.Get(typeof(IInterface_1), null), Times.Once);
        }

        private interface IServiceHavingNonInterfaceCtorArg<T>
        {
            string CtorParam { get; }
        }

        private class ServiceHavingNonInterfaceCtorArg<T> : IServiceHavingNonInterfaceCtorArg<T>
        {
            public ServiceHavingNonInterfaceCtorArg(string para, IInterface_1 unused) => CtorParam = para;

            public string CtorParam { get; }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Service_ShouldHandleExplicitArgsInCaseOfGenericServiceToo(Lifetime lifetime)
        {
            Container.Service(typeof(IServiceHavingNonInterfaceCtorArg<>), typeof(ServiceHavingNonInterfaceCtorArg<>), new Dictionary<string, object> { ["para"] = "cica" }, lifetime);

            Assert.That(Container.Get(typeof(IServiceHavingNonInterfaceCtorArg<>)).Factory, Is.Null);

            Func<IInjector, Type, object> fact = Container.Get<IServiceHavingNonInterfaceCtorArg<object>>(QueryModes.AllowSpecialization).Factory;

            Assert.That(fact, Is.Not.Null);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IInterface_1), null))
                .Returns(null);

            IServiceHavingNonInterfaceCtorArg<object> myService = (IServiceHavingNonInterfaceCtorArg<object>) fact(mockInjector.Object, typeof(IServiceHavingNonInterfaceCtorArg<object>));

            Assert.That(myService.CtorParam, Is.EqualTo("cica"));

            mockInjector.Verify(i => i.Get(typeof(IInterface_1), null), Times.Once);
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Service_ShouldHandleClosedGenericTypes(Lifetime lifetime)
        {
            Container
                .Service<IInterface_3<int>, Implementation_3_IInterface_1_Dependant<int>>(lifetime);

            Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_3<int>), null, typeof(Implementation_3_IInterface_1_Dependant<int>), Container), Container.Get<IInterface_3<int>>());
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Service_ShouldThrowOnMultipleConstructors(Lifetime lifetime)
        {
            Assert.Throws<NotSupportedException>(
                () => Container.Service<IList<int>, List<int>>(lifetime),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<int>)));

            Assert.Throws<NotSupportedException>(
                () => Container.Service(typeof(IList<>), typeof(List<>), lifetime),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<>)));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Service_ShouldBeInstructedByServiceActivator(Lifetime lifetime)
        {
            Assert.DoesNotThrow(() => Container.Service<IInterface_1, Implementation_8_MultiCtor>(lifetime));
            Assert.DoesNotThrow(() => Container.Service(typeof(IInterface_3<>), typeof(Implementation_9_MultiCtor<>), lifetime));

            ConstructorInfo ctor = typeof(Implementation_8_MultiCtor).GetConstructor(new Type[0]);

            Assert.That(Container.Get<IInterface_1>().Factory, Is.EqualTo(Resolver.Get(ctor)));

            ctor = typeof(Implementation_9_MultiCtor<int>).GetConstructor(new Type[] { typeof(IInterface_1) });

            Assert.That(Container.Get<IInterface_3<int>>(QueryModes.AllowSpecialization).Factory, Is.EqualTo(Resolver.Get(ctor)));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Container_Service_ShouldThrowOnConstructorHavingNonInterfaceArgument(Lifetime lifetime)
        {
            Assert.Throws<ArgumentException>(() => Container.Service<IInterface_1, Implementation_1_Non_Interface_Dep>(lifetime));
        }

        public void Container_Service_ShouldThrowOnMultipleRegistration([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime1);

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime2));
        }
    }
}
