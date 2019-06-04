/********************************************************************************
* CreateChild.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void Injector_ChildShouldInheritTheParentEntries()
        {
            IInjector child = Injector
                .Service<IInterface_1, Implementation_1>()
                .CreateChild();

            IInterface_1 inst = null;

            Assert.DoesNotThrow(() => inst = child.Get<IInterface_1>());
            Assert.That(inst, Is.InstanceOf<Implementation_1>());
        }

        [Test]
        public void Injector_ChildShouldInheritTheProxies()
        {
            IInjector child = Injector
                .Service<IInterface_1, Implementation_1>()
                .Proxy<IInterface_1>((me, val) => new DecoratedImplementation_1())
                .CreateChild();

            IInterface_1 inst = null;

            Assert.DoesNotThrow(() => inst = child.Get<IInterface_1>());
            Assert.That(inst, Is.InstanceOf<DecoratedImplementation_1>());
        }

        [Test]
        public void Injector_ChildShouldBeIndependentFromTheParent()
        {
            IInjector child = Injector
                .CreateChild()
                .Service<IInterface_1, Implementation_1>();

            IInterface_1 inst = null;

            Assert.DoesNotThrow(() => inst = child.Get<IInterface_1>());
            Assert.That(inst, Is.InstanceOf<Implementation_1>());
            Assert.Throws<NotSupportedException>(() => Injector.Get<IInterface_1>(), string.Format(Resources.DEPENDENCY_NOT_FOUND, typeof(IInterface_1)));
        }

        [Test]
        public void Injector_ChildShouldRecreateTheSingletons()
        {
            IInjector child = Injector
                .Service<IInterface_1, Implementation_1>(Lifetime.Singleton)
                .CreateChild();

            Assert.AreSame(Injector.Get<IInterface_1>(), Injector.Get<IInterface_1>());
            Assert.AreSame(child.Get<IInterface_1>(), child.Get<IInterface_1>());
            Assert.AreNotSame(Injector.Get<IInterface_1>(), child.Get<IInterface_1>());
        }

        [Test]
        public void Injector_ChildShouldResolveItself()
        {
            IInjector child = Injector.CreateChild();

            Assert.AreSame(Injector, Injector.Get<IInjector>());
            Assert.AreSame(child, child.Get<IInjector>());
            Assert.AreNotSame(Injector, child);
        }

        [Test]
        public void Injector_ChildShouldPassItselfToItsFactories()
        {
            Injector
                .Service<IInterface_1, Implementation_1>()
                .Service<IInterface_2, Implementation_2>();

            using (IInjector child = Injector.CreateChild())
            {
                child.Proxy<IInterface_1>((injector, type) =>
                {
                    Assert.AreSame(injector, child);

                    return new DecoratedImplementation_1();
                });

                Assert.That(Injector.Get<IInterface_2>().Interface1, Is.InstanceOf<Implementation_1>());
                Assert.That(child.Get<IInterface_2>().Interface1, Is.InstanceOf<DecoratedImplementation_1>());
            }
        }

        [Test]
        public void Injector_CreateChild_ShouldNotTriggerTheTypeResolver()
        {
            var mockTypeResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockTypeResolver.Setup(i => i.Resolve(It.IsAny<Type>()));

            Injector.Lazy<IInterface_1>(mockTypeResolver.Object);

            using (Injector.CreateChild())
            {
            }

            mockTypeResolver.Verify(i => i.Resolve(It.IsAny<Type>()), Times.Never);
        }
    }
}
