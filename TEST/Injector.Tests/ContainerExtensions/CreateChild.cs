/********************************************************************************
* CreateChild.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    public partial class ContainerTestsBase<TContainer>
    {
        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void IServiceContainer_InheritanceShouldChangeTheOwnerOnly(Lifetime lifetime)
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime)
                .Factory<IInterface_2>(i => new Implementation_2_IInterface_1_Dependant(i.Get<IInterface_1>()), lifetime)
                .Instance((IDisposable)new Disposable());

            IServiceContainer child = Container.CreateChild();

            Assert.AreEqual(GetHashCode(Container.Get<IInterface_1>(QueryModes.ThrowOnError)), GetHashCode(child.Get<IInterface_1>(QueryModes.ThrowOnError)));
            Assert.AreEqual(GetHashCode(Container.Get<IInterface_2>(QueryModes.ThrowOnError)), GetHashCode(child.Get<IInterface_2>(QueryModes.ThrowOnError)));
            Assert.AreEqual(GetHashCode(Container.Get<IDisposable>(QueryModes.ThrowOnError)), GetHashCode(child.Get<IDisposable>(QueryModes.ThrowOnError)));

            int GetHashCode(AbstractServiceEntry info) => new
            {
                //
                // Owner NE szerepeljen
                //

                info.Interface,
                info.Lifetime,
                info.Factory,
                info.Instance,
                info.Implementation
            }.GetHashCode();
        }

        [Test]
        public void Container_CreateChild_ShouldThrowIfChildCountReachedTheLimit()
        {
            Config.Value.Composite.MaxChildCount = 1;

            //
            // this.Container-re mar nem lesz alkalmazva a MaxChildCount
            //

            using (var container = new ServiceContainer())
            {
                Assert.DoesNotThrow(() => container.CreateChild());
                Assert.Throws<InvalidOperationException>(() => container.CreateChild());
            }
        }

        [Test]
        public void Container_ChildShouldInheritEntriesFromTheParent()
        {
            IServiceContainer child = Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .CreateChild();

            Assert.DoesNotThrow(() => child.Get<IInterface_1>(QueryModes.ThrowOnError));
        }

        [Test]
        public void Container_ChildShouldInheritProxies()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>();

            Func<IInjector, Type, object>
                originalFactory = Container.Get<IInterface_1>(QueryModes.ThrowOnError).Factory,
                proxiedFactory = Container
                    .Proxy<IInterface_1>((me, val) => new DecoratedImplementation_1())
                    .Get<IInterface_1>(QueryModes.ThrowOnError)
                    .Factory;

            Assert.AreNotSame(originalFactory, proxiedFactory);

            IServiceContainer child = Container.CreateChild();
            Assert.AreSame(proxiedFactory, child.Get<IInterface_1>(QueryModes.ThrowOnError).Factory);
        }

        [Test]
        public void Container_ChildShouldBeIndependentFromTheParent()
        {
            IServiceContainer child = Container
                .CreateChild()
                .Service<IInterface_1, Implementation_1_No_Dep>();

            Assert.Throws<ServiceNotFoundException>(() => Container.Get<IInterface_1>(QueryModes.ThrowOnError));
            Assert.DoesNotThrow(() => child.Get<IInterface_1>(QueryModes.ThrowOnError));
        }

        [Test]
        public void Container_CreateChild_ShouldCopyTheEntriesFromItsParent()
        {
            //
            // MockBehavior ne legyen megadva h mikor a GC felszabaditja a mock entitast
            // akkor az ne hasaljon el azert mert a Dispose(bool)-ra (ami egyebkent vedett
            // tag) nem volt hivva a Setup().
            //

            Mock<AbstractServiceEntry> entry = new Mock<AbstractServiceEntry>(typeof(IDisposable) /*iface*/, null, Container);
            entry.Setup(e => e.CopyTo(It.IsAny<IServiceContainer>())).Returns<IServiceContainer>(sc => null);

            Container.Add(entry.Object);

            using (IServiceContainer container = Container.CreateChild())
            {
                entry.Verify(e => e.CopyTo(It.Is<IServiceContainer>(sc => sc == container)), Times.Once);
            }
        }

        [Test]
        public void Container_CreateChild_ShouldNotAddTheChildIfSomethingWentWrong() 
        {
            Container.Add(new BadServiceEntry(typeof(IInterface_1), null, Container));

            Assert.Throws<InvalidOperationException>(() => Container.CreateChild());
            Assert.That(Container.Children.Count, Is.EqualTo(0));
        }

        private sealed class BadServiceEntry : AbstractServiceEntry
        {
            public BadServiceEntry(Type @interface, string name, IServiceContainer owner) : base(@interface, name, owner)
            {
            }

            public override AbstractServiceEntry CopyTo(IServiceContainer target) => throw new InvalidOperationException();
        }
    }
}
