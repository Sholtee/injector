using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
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
    }
}
