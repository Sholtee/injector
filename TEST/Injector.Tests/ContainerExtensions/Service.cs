/********************************************************************************
* Service.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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
            Assert.Throws<ArgumentNullException>(() => IServiceContainerAdvancedExtensions.Service(null, typeof(IDisposable), typeof(Disposable)));
            Assert.Throws<ArgumentNullException>(() => Container.Service(null, typeof(Disposable)));
            Assert.Throws<ArgumentNullException>(() => Container.Service(typeof(IDisposable), null));
        }

        [Test]
        public void Container_Service_ShouldThrowOnNonInterfaceKey()
        {
            Assert.Throws<ArgumentException>(() => Container.Service<Object, Object>(), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
            Assert.Throws<ArgumentException>(() => Container.Service(typeof(Object), typeof(Object)), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Singleton)]
        public void Container_Service_ShouldHandleGenericTypes(Lifetime lifetime)
        {
            Container
                .Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), lifetime);

            AbstractServiceEntry entry = Container.Get<IInterface_3<int>>(QueryModes.AllowSpecialization);

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Interface, Is.EqualTo(typeof(IInterface_3<int>)));
            Assert.That(entry.Implementation, Is.EqualTo(typeof(Implementation_3_IInterface_1_Dependant<int>)));
            Assert.That(entry.Lifetime, Is.EqualTo(lifetime));
        }

        [Test]
        public void Container_Service_ShouldHandleNamedServices() 
        {
            Assert.DoesNotThrow(() => Container
                .Service<IInterface_1, Implementation_1_No_Dep>("svc1")
                .Service<IInterface_1, DecoratedImplementation_1>("svc2"));

            Assert.IsNull(Container.Get<IInterface_1>());
            Assert.IsNull(Container.Get<IInterface_1>("invalidname"));

            Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_1), "svc1", typeof(Implementation_1_No_Dep), Container), Container.Get<IInterface_1>("svc1"));
            Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_1), "svc2", typeof(DecoratedImplementation_1), Container), Container.Get<IInterface_1>("svc2"));
        }

        [Test]
        public void Container_Service_ShouldHandleClosedGenericTypes()
        {
            Container
                .Service<IInterface_3<int>, Implementation_3_IInterface_1_Dependant<int>>();

            Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_3<int>), null, typeof(Implementation_3_IInterface_1_Dependant<int>), Container), Container.Get<IInterface_3<int>>());
        }

        [Test]
        public void Container_Service_ShouldThrowOnMultipleConstructors()
        {
            Assert.Throws<NotSupportedException>(
                () => Container.Service<IList<int>, List<int>>(),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<int>)));

            Assert.Throws<NotSupportedException>(
                () => Container.Service(typeof(IList<>), typeof(List<>)),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<>)));
        }

        [Test]
        public void Container_Service_ShouldBeInstructedByServiceActivator()
        {
            Assert.DoesNotThrow(() => Container.Service<IInterface_1, Implementation_8_MultiCtor>());
            Assert.DoesNotThrow(() => Container.Service(typeof(IInterface_3<>), typeof(Implementation_9_MultiCtor<>)));

            ConstructorInfo ctor = typeof(Implementation_8_MultiCtor).GetConstructor(new Type[0]);

            Assert.That(Container.Get<IInterface_1>().Factory, Is.EqualTo(Resolver.Get(ctor)));

            ctor = typeof(Implementation_9_MultiCtor<int>).GetConstructor(new Type[] { typeof(IInterface_1) });

            Assert.That(Container.Get<IInterface_3<int>>(QueryModes.AllowSpecialization).Factory, Is.EqualTo(Resolver.Get(ctor)));
        }

        [Test]
        public void Container_Service_ShouldThrowOnConstructorHavingNonInterfaceArgument()
        {
            Assert.Throws<ArgumentException>(() => Container.Service<IInterface_1, Implementation_1_Non_Interface_Dep>());
        }

        public static IEnumerable<(Type Interface, Type Implementation)> BadRegistrations // sima TestCase-el nem fog mukodni
        {
            get 
            {
                yield return (typeof(IInterface_2), typeof(Implementation_1_No_Dep));
                yield return (typeof(IList<>), typeof(Implementation_1_No_Dep));
                yield return (typeof(IList<int>), typeof(List<string>));
                yield return (typeof(IList<int>), typeof(List<>));
                yield return (typeof(IList<>), typeof(List<string>));
            }
        }

        [TestCaseSource(nameof(BadRegistrations))]
        public void Container_Service_ShouldThrowIfTheInterfaceIsNotAssignableFromTheImplementation((Type Interface, Type Implementation) para) =>
            Assert.Throws<ArgumentException>(() => Container.Service(para.Interface, para.Implementation), string.Format(Interfaces.Properties.Resources.INTERFACE_NOT_SUPPORTED, para.Interface));

        [Test]
        public void Container_Service_ShouldThrowOnMultipleRegistration()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>();

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Service<IInterface_1, Implementation_1_No_Dep>());
        }


        [Test]
        public void Container_Service_CanBeRegisteredViaAttribute()
        {
            Container.Setup(typeof(ConcreteService).Assembly, "Solti.Utils.DI.Container.Tests");

            AbstractServiceEntry entry = Container.Get<IList<int>>();

            Assert.IsNotNull(entry);
            Assert.That(entry.Lifetime, Is.EqualTo(Lifetime.Singleton));
            Assert.That(entry.Factory(new Mock<IInjector>(MockBehavior.Strict).Object, typeof(IDisposable)), Is.InstanceOf<ConcreteService>());
        }
    }

    [Service(typeof(IList<int>), Lifetime.Singleton)]
    public class ConcreteService : List<int> { } // ne nested legyen mert akkor generikusnak minosul (ContainerTestsBase<TContainer>.ConcreteService)
}
