﻿/********************************************************************************
* Service.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;
    using Primitives;
    using Primitives.Patterns;
    using Properties;
    
    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public void Service_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionBasicExtensions.Service(null, typeof(IDisposable), typeof(Disposable), Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Collection.Service(null, typeof(Disposable), Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Collection.Service(typeof(IDisposable), null, Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Collection.Service(typeof(IDisposable), typeof(Disposable), null));
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionBasicExtensions.Service(null, typeof(IDisposable), typeof(Disposable), new { }, Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Collection.Service(null, typeof(Disposable), new { }, Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Collection.Service(typeof(IDisposable), null, new { }, Lifetime.Transient));
            Assert.Throws<ArgumentNullException>(() => Collection.Service(typeof(IDisposable), typeof(Disposable), new { }, null));
            Assert.Throws<ArgumentNullException>(() => Collection.Service(typeof(IDisposable), typeof(Disposable), explicitArgs: null, Lifetime.Transient));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldThrowOnNonClassImplementation(Lifetime lifetime)
        {
            Assert.Throws<ArgumentException>(() => Collection.Service(typeof(IDisposable), key: null, typeof(int), lifetime), string.Format(Resources.PARAMETER_NOT_A_CLASS, "implementation"));
            Assert.Throws<ArgumentException>(() => Collection.Service(typeof(IDisposable), key: null, typeof(int), new object(), lifetime), string.Format(Resources.PARAMETER_NOT_A_CLASS, "implementation"));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldThrowOnMissingImplementation(Lifetime lifetime)
        {
            Assert.Throws<ArgumentException>(() => Collection.Service(typeof(IDisposable), key: null, typeof(object), lifetime), string.Format(Resources.PARAMETER_NOT_A_CLASS, "implementation"));
            Assert.Throws<ArgumentException>(() => Collection.Service(typeof(IDisposable), key: null, typeof(object), new object(), lifetime), string.Format(Resources.PARAMETER_NOT_A_CLASS, "implementation"));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldHandleGenericTypes_Iface(Lifetime lifetime)
        {
            Collection.Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), lifetime);

            AbstractServiceEntry entry = Collection.Last().Specialize(typeof(int));

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Type, Is.EqualTo(typeof(IInterface_3<int>)));
            Assert.That(entry.Implementation, Is.EqualTo(typeof(Implementation_3_IInterface_1_Dependant<int>)));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldHandleGenericTypes(Lifetime lifetime)
        {
            Collection.Service(typeof(Implementation_3_IInterface_1_Dependant<>), lifetime);

            AbstractServiceEntry entry = Collection.Last().Specialize(typeof(int));

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Type, Is.EqualTo(typeof(Implementation_3_IInterface_1_Dependant<int>)));
            Assert.That(entry.Implementation, Is.EqualTo(typeof(Implementation_3_IInterface_1_Dependant<int>)));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldHandleNamedServices(Lifetime lifetime) 
        {
            Assert.DoesNotThrow(() => Collection.Service<IInterface_1, Implementation_1_No_Dep>("svc1", lifetime));
            Assert.That(Collection.Last().Key, Is.EqualTo("svc1"));
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
        public void Service_ShouldHandleExplicitArgs(Lifetime lifetime)
        {
            Collection.Service<IServiceHavingNonInterfaceCtorArg, ServiceHavingNonInterfaceCtorArg>(null, new Dictionary<string, object> { ["para"] = "cica" }, lifetime);

            Expression<FactoryDelegate> fact = Collection.Last().Factory;

            Assert.That(fact, Is.Not.Null);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IInterface_1), null))
                .Returns(null);

            IServiceHavingNonInterfaceCtorArg myService = (IServiceHavingNonInterfaceCtorArg) fact.Compile().Invoke(mockInjector.Object, typeof(IServiceHavingNonInterfaceCtorArg));

            Assert.That(myService.CtorParam, Is.EqualTo("cica"));

            mockInjector.Verify(i => i.Get(typeof(IInterface_1), null), Times.Once);
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldHandleExplicitArgs2(Lifetime lifetime)
        {
            Collection.Service<IServiceHavingNonInterfaceCtorArg, ServiceHavingNonInterfaceCtorArg>(null, new { para = "cica" }, lifetime);

            Expression<FactoryDelegate> fact = Collection.Last().Factory;

            Assert.That(fact, Is.Not.Null);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IInterface_1), null))
                .Returns(null);

            IServiceHavingNonInterfaceCtorArg myService = (IServiceHavingNonInterfaceCtorArg) fact.Compile().Invoke(mockInjector.Object, typeof(IServiceHavingNonInterfaceCtorArg));

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
        public void Service_ShouldHandleExplicitArgsInCaseOfGenericService(Lifetime lifetime)
        {
            Collection.Service(typeof(IServiceHavingNonInterfaceCtorArg<>), typeof(ServiceHavingNonInterfaceCtorArg<>), new Dictionary<string, object> { ["para"] = "cica" }, lifetime);

            Expression<FactoryDelegate> fact = Collection.Last().Specialize(typeof(object)).Factory;

            Assert.That(fact, Is.Not.Null);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IInterface_1), null))
                .Returns(null);

            IServiceHavingNonInterfaceCtorArg<object> myService = (IServiceHavingNonInterfaceCtorArg<object>) fact.Compile().Invoke(mockInjector.Object, typeof(IServiceHavingNonInterfaceCtorArg<object>));

            Assert.That(myService.CtorParam, Is.EqualTo("cica"));

            mockInjector.Verify(i => i.Get(typeof(IInterface_1), null), Times.Once);
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldHandleExplicitArgsInCaseOfGenericService2(Lifetime lifetime)
        {
            Collection.Service(typeof(IServiceHavingNonInterfaceCtorArg<>), typeof(ServiceHavingNonInterfaceCtorArg<>), new { para = "cica" }, lifetime);

            Expression<FactoryDelegate> fact = Collection.Last().Specialize(typeof(object)).Factory;

            Assert.That(fact, Is.Not.Null);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IInterface_1), null))
                .Returns(null);

            IServiceHavingNonInterfaceCtorArg<object> myService = (IServiceHavingNonInterfaceCtorArg<object>) fact.Compile().Invoke(mockInjector.Object, typeof(IServiceHavingNonInterfaceCtorArg<object>));

            Assert.That(myService.CtorParam, Is.EqualTo("cica"));

            mockInjector.Verify(i => i.Get(typeof(IInterface_1), null), Times.Once);
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldHandleClosedGenericTypes_Iface(Lifetime lifetime)
        {
            Assert.DoesNotThrow(() => Collection.Service<IInterface_3<int>, Implementation_3_IInterface_1_Dependant<int>>(lifetime));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldHandleClosedGenericTypes(Lifetime lifetime)
        {
            Assert.DoesNotThrow(() => Collection.Service<Implementation_3_IInterface_1_Dependant<int>>(lifetime));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldThrowOnMultipleConstructors(Lifetime lifetime)
        {
            Assert.Throws<NotSupportedException>(
                () => Collection.Service<IList<int>, List<int>>(lifetime),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<int>)));

            Assert.Throws<NotSupportedException>(
                () => Collection.Service(typeof(IList<>), typeof(List<>), lifetime),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<>)));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldAssignAFactoryFunction_Iface(Lifetime lifetime)
        {
            Assert.DoesNotThrow(() => Collection.Service<IInterface_1, Implementation_8_MultiCtor>(lifetime));
            Assert.That(Collection.Last().Factory.GetDebugView(), Is.EqualTo(new ServiceActivator(null).ResolveFactory(typeof(Implementation_8_MultiCtor).GetConstructor(Type.EmptyTypes), null).GetDebugView()));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldAssignAFactoryFunction(Lifetime lifetime)
        {
            Assert.DoesNotThrow(() => Collection.Service<Implementation_8_MultiCtor>(lifetime));
            Assert.That(Collection.Last().Factory.GetDebugView(), Is.EqualTo(new ServiceActivator(null).ResolveFactory(typeof(Implementation_8_MultiCtor).GetConstructor(Type.EmptyTypes), null).GetDebugView()));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldAssignAFactoryFunction_Generic(Lifetime lifetime)
        {
            Assert.DoesNotThrow(() => Collection.Service(typeof(IInterface_3<>), typeof(Implementation_9_MultiCtor<>), lifetime));
            Assert.That(Collection.Last().Specialize(typeof(int)).Factory.GetDebugView(), Is.EqualTo(new ServiceActivator(null).ResolveFactory(typeof(Implementation_9_MultiCtor<int>).GetConstructor(new Type[] { typeof(IInterface_1) }), null).GetDebugView()));
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Service_ShouldThrowOnConstructorHavingNonInterfaceArgument(Lifetime lifetime)
        {
            Assert.Throws<ArgumentException>(() => Collection.Service<IInterface_1, Implementation_1_Non_Interface_Dep>(lifetime));
        }

        [Test]
        public void Service_ShouldThrowOnMultipleRegistration_Iface([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Collection.Service<IInterface_1, Implementation_1_No_Dep>(lifetime1);

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Service<IInterface_1, Implementation_1_No_Dep>(lifetime2));
        }

        [Test]
        public void Service_ShouldThrowOnMultipleRegistration([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Collection.Service<Implementation_1_No_Dep>(lifetime1);

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Collection.Service<Implementation_1_No_Dep>(lifetime2));
        }
    }
}
