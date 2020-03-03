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
    using Internals;
    using Properties;

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

            Assert.DoesNotThrow(() => Container.CreateChild());
            Assert.Throws<InvalidOperationException>(() => Container.CreateChild(), Resources.TOO_MANY_CHILDREN);
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

            Mock<AbstractServiceEntry> entry = new Mock<AbstractServiceEntry>(typeof(IDisposable) /*iface*/, null, Lifetime.Transient, null);
            entry.Setup(e => e.CopyTo(It.IsAny<IServiceContainer>())).Returns<IServiceContainer>(sc => null);

            using (IServiceContainer container = Container.Add(entry.Object).CreateChild())
            {
                entry.Verify(e => e.CopyTo(It.Is<IServiceContainer>(sc => sc == container)), Times.Once);
            }
        }

        [Test]
        public void Container_CreateChild_ShouldNotTriggerTheTypeResolver()
        {
            var mockTypeResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockTypeResolver.Setup(i => i.Resolve(It.IsAny<Type>())).Returns<Type>(null);
            mockTypeResolver.Setup(i => i.Supports(It.IsAny<Type>())).Returns(true);

            Container.Lazy<IInterface_1>(mockTypeResolver.Object);

            using (Container.CreateChild())
            {
            }

            mockTypeResolver.Verify(i => i.Resolve(It.IsAny<Type>()), Times.Never);
        }

        [Test]
        public void Container_CreateChild_ShouldNotAddTheChildIfSomethingWentWrong() 
        {
            Container.Add(new BadServiceEntry(typeof(IInterface_1), null));

            Assert.Throws<Exception>(() => Container.CreateChild());
            Assert.That(Container.Children.Count, Is.EqualTo(0));
        }

        private sealed class BadServiceEntry : AbstractServiceEntry
        {
            public BadServiceEntry(Type @interface, string name) : base(@interface, name)
            {
            }

            public override AbstractServiceEntry CopyTo(IServiceContainer target) => throw new Exception();
        }
    }
}
