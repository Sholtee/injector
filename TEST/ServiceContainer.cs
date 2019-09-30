/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

using Solti.Utils.DI.Tests;

namespace Solti.Utils.DI.Container.Tests
{   
    using Internals;

    public abstract class ServiceContainerTestsBase<TImplementation>: TestBase<TImplementation> where TImplementation : IServiceContainer, new()
    {
        internal virtual IServiceContainer CreateContainer(params AbstractServiceEntry[] entries)
        {
            var result = new TImplementation();

            foreach (AbstractServiceEntry entry in entries)
            {
                result.Add(entry);
            }

            return result;
        }

        [Test]
        public void IServiceContainer_DisposeShouldDisposeOwnedEntriesOnly()
        {
            Disposable 
                owned    = new Disposable(),
                notOwned = new Disposable();

            IServiceContainer container = CreateContainer();

            container.Add(new InstanceServiceEntry(typeof(IDisposable), owned, releaseOnDispose: true, owner: container));
            container.Add(new InstanceServiceEntry(typeof(IServiceContainer) /*tok mind1*/, notOwned, releaseOnDispose: true, owner: null));

            Assert.That(container.Count, Is.EqualTo(2));

            container.Dispose();
            
            Assert.That(owned.Disposed);
            Assert.That(notOwned.Disposed, Is.False);
        }

        [Test]
        public void IServiceContainer_CreateChildShouldCopyTheInheritedEntries()
        {
            //
            // MockBehavior ne legyen megadva h mikor a GC felszabaditja a mock entitast
            // akkor az ne hasaljon el azert mert a Dispose(bool)-ra (ami egyebkent vedett
            // tag) nem volt hivva a Setup().
            //

            Mock<AbstractServiceEntry> entry = new Mock<AbstractServiceEntry>(typeof(IDisposable) /*iface*/, Lifetime.Transient, new TImplementation());              
            entry.Setup(e => e.CopyTo(It.IsAny<IServiceContainer>())).Returns<IServiceContainer>(sc => null);

            using (IServiceContainer container = CreateContainer(entry.Object).CreateChild())
            {
                entry.Verify(e => e.CopyTo(It.Is<IServiceContainer>(sc => sc == container)), Times.Once);
            }
        }

        [Test]
        public void IServiceContainer_ShouldContainUniqueEntries()
        {
            IServiceContainer container = CreateContainer
            (
                new InstanceServiceEntry(typeof(IDisposable), null, false, null)
            );

            Assert.Throws<ServiceAlreadyRegisteredException>(() => container.Add(new InstanceServiceEntry(typeof(IDisposable), null, false, null)));

            Assert.Throws<ServiceAlreadyRegisteredException>(() => CreateContainer
            (
                new InstanceServiceEntry(typeof(IDisposable), null, false, null),
                new InstanceServiceEntry(typeof(IDisposable), null, false, null)
            ));
        }

        [Test]
        public void IServiceContainer_GetShouldReturnOnTypeMatch()
        {          
            IServiceContainer container = CreateContainer();
            var entry = new TransientServiceEntry(typeof(IList<>), typeof(List<>), container);
            container.Add(entry);

            Assert.That(container.Get(typeof(IList<>), QueryMode.ThrowOnError), Is.EqualTo(entry));
        }

        [Test]
        public void IServiceContainer_GetShouldReturnTheSpecializedEntry()
        {
            IServiceContainer container = CreateContainer();

            //
            // Azert singleton h az owner kontener ne valtozzon.
            //
            
            container.Add(new SingletonServiceEntry(typeof(IList<>), typeof(MyList<>), container));

            Assert.That(container.Count, Is.EqualTo(1));
            Assert.Throws<ServiceNotFoundException>(() => container.Get(typeof(IList<int>), QueryMode.ThrowOnError));
            Assert.That(container.Count, Is.EqualTo(1));
            Assert.That(container.Get(typeof(IList<int>), QueryMode.AllowSpecialization | QueryMode.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), typeof(MyList<int>), container)));
            Assert.That(container.Count, Is.EqualTo(2));
        }

        [Test]
        public void IServiceContainer_GetShouldReturnTheSpecializedInheritedEntry()
        {
            IServiceContainer container = CreateContainer();
            container.Add(new SingletonServiceEntry(typeof(IList<>), typeof(MyList<>), container));

            using (IServiceContainer child = container.CreateChild())
            {
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.Throws<ServiceNotFoundException>(() => child.Get(typeof(IList<int>), QueryMode.ThrowOnError));
                Assert.That(container.Count, Is.EqualTo(1));
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.That(child.Get(typeof(IList<int>), QueryMode.AllowSpecialization | QueryMode.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), typeof(MyList<int>), container)));
                Assert.That(container.Count, Is.EqualTo(2));
                Assert.That(child.Count, Is.EqualTo(2));
            }

            Assert.That(container.Get(typeof(IList<int>), QueryMode.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), typeof(MyList<int>), container)));
        }

        [Test]
        public void IServiceContainer_GetShouldReturnExistingEntriesOnly()
        {
            IServiceContainer container = CreateContainer
            (
                new SingletonServiceEntry(typeof(IList<>), typeof(List<>), null) 
            );

            Assert.IsNull(container.Get(typeof(IList<int>)));
            Assert.Throws<ServiceNotFoundException>(() => container.Get(typeof(IList<int>), QueryMode.ThrowOnError));
            Assert.That(container.Get(typeof(IList<>), QueryMode.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<>), typeof(List<>), null)));
        }

        [Test]
        public void IServiceContainer_ContainsShouldSearchByGetHashCode()
        {
            AbstractServiceEntry 
                entry1 = new AbstractServiceEntry(typeof(IDisposable)),
                entry2 = new AbstractServiceEntry(typeof(IDisposable));

            IServiceContainer container = CreateContainer(entry1);
            
            Assert.That(entry1, Is.EqualTo(entry2));
            Assert.True(container.Contains(entry1));
            Assert.True(container.Contains(entry2));
        }


        [Test]
        public void IServiceContainer_EnumeratorShouldBeIndependent()
        {
            var entry = new AbstractServiceEntry(typeof(IDisposable));

            IServiceContainer container = CreateContainer(entry);

            using (IEnumerator<AbstractServiceEntry> enumerator = container.GetEnumerator())
            {
                container.Add(new AbstractServiceEntry(typeof(IList<>)));
                Assert.That(enumerator.MoveNext);
                Assert.AreSame(enumerator.Current, entry);
                Assert.False(enumerator.MoveNext());
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void IServiceContainer_DisposeShouldFreeInstancesIfReleaseOnDisposeWasSetToTrue(bool releaseOnDispose)
        {
            var mockInstance = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockInstance.Setup(i => i.Dispose());

            using (IServiceContainer child = Container.CreateChild())
            {
                child.Instance(mockInstance.Object, releaseOnDispose);

                using (child.CreateChild())
                {
                }

                mockInstance.Verify(i => i.Dispose(), Times.Never);
            }

            mockInstance.Verify(i => i.Dispose(), releaseOnDispose ? Times.Once : (Func<Times>)Times.Never);
        }

        [Test]
        public void IServiceContainer_ShouldKeepUpToDateTheChildrenList()
        {
            Assert.That(Container.Children, Is.Empty);

            using (IServiceContainer child = Container.CreateChild())
            {
                Assert.That(Container.Children.Count, Is.EqualTo(1));
                Assert.AreSame(Container.Children.First(), child);
            }

            Assert.That(Container.Children, Is.Empty);
        }

        [Test]
        public void IServiceContainer_DisposeShouldDisposeChildContainerAndItsEntries()
        {
            IServiceContainer grandChild;
            IDisposable instance;

            using (IServiceContainer child = Container.CreateChild())
            {
                grandChild = child.CreateChild().Instance(instance = new Disposable(), releaseOnDispose: true);
            }

            Assert.Throws<ObjectDisposedException>(grandChild.Dispose);
            Assert.Throws<ObjectDisposedException>(instance.Dispose);
        }

        [Test]
        public void IServiceContainer_DisposeShouldDisposeSpecializedEntries()
        {
            Disposable testDisposable;

            using (IServiceContainer child = Container.CreateChild())
            {
                child
                    .Service<IInterface_1, Implementation_1>()
                    .Service(typeof(IGenericDisposable<>), typeof(GenericDisposable<>), Lifetime.Singleton);

                Assert.That(child.Count, Is.EqualTo(2));
                Assert.AreSame(child.CreateInjector().Get<IGenericDisposable<int>>(), child.CreateInjector().Get<IGenericDisposable<int>>());
                Assert.That(child.Count, Is.EqualTo(3));

                testDisposable = (Disposable) child.CreateInjector().Get<IGenericDisposable<int>>();
                Assert.That(testDisposable.Disposed, Is.False);
            }

            Assert.That(testDisposable.Disposed, Is.True);
        }

        public interface IGenericDisposable<T> : IDisposable
        {
        }

        public class GenericDisposable<T> : Disposable, IGenericDisposable<T>
        {
        }

        [Test]
        public void IServiceContainer_DisposeShouldEatExceptions()
        {
            IServiceContainer child = Container
                .CreateChild()
                .Instance<IDisposable>(new BadDisposable(), releaseOnDispose: true);

            Assert.DoesNotThrow(() => child.Dispose());
        }

        private sealed class BadDisposable: Disposable
        {
            protected override void Dispose(bool disposeManaged) => throw new Exception();
        }

        [Test]
        public void IServiceContainer_ChildShouldInheritTheParentEntries()
        {
            IServiceContainer child = Container
                .Service<IInterface_1, Implementation_1>()
                .CreateChild();

            Assert.That(child.Get<IInterface_1>(QueryMode.ThrowOnError).Implementation, Is.EqualTo(typeof(Implementation_1)));
        }

        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void IServiceContainer_ChildEnriesShouldNotChangeByDefault(Lifetime lifetime)
        {
            Container
                .Service<IInterface_1, Implementation_1>(lifetime)
                .Factory<IInterface_2>(i => new Implementation_2(i.Get<IInterface_1>()), lifetime)
                .Instance<IDisposable>(new Disposable());

            IServiceContainer child = Container.CreateChild();

            Assert.AreEqual(GetHashCode(Container.Get<IInterface_1>(QueryMode.ThrowOnError)), GetHashCode(child.Get<IInterface_1>(QueryMode.ThrowOnError)));
            Assert.AreEqual(GetHashCode(Container.Get<IInterface_2>(QueryMode.ThrowOnError)), GetHashCode(child.Get<IInterface_2>(QueryMode.ThrowOnError)));
            Assert.AreEqual(GetHashCode(Container.Get<IDisposable>(QueryMode.ThrowOnError)), GetHashCode(child.Get<IDisposable>(QueryMode.ThrowOnError)));

            int GetHashCode(AbstractServiceEntry info) => new
            {
                //
                // Owner NE szerepeljen
                //

                info.Interface,
                info.Lifetime,
                info.Factory,
                info.Value,
                info.Implementation
            }.GetHashCode();
        }

        [Test]
        public void IServiceContainer_ChildShouldInheritTheProxies()
        {
            Container.Service<IInterface_1, Implementation_1>();

            Func<IInjector, Type, object>
                originalFactory = Container.Get<IInterface_1>(QueryMode.ThrowOnError).Factory,
                proxiedFactory = Container
                    .Proxy<IInterface_1>((me, val) => new DecoratedImplementation_1())
                    .Get<IInterface_1>(QueryMode.ThrowOnError)
                    .Factory;

            Assert.AreNotSame(originalFactory, proxiedFactory);

            IServiceContainer child = Container.CreateChild();
            Assert.AreSame(proxiedFactory, child.Get<IInterface_1>(QueryMode.ThrowOnError).Factory);
        }

        [Test]
        public void IServiceContainer_ChildShouldBeIndependentFromTheParent()
        {
            IServiceContainer child = Container
                .CreateChild()
                .Service<IInterface_1, Implementation_1>();

            Assert.Throws<ServiceNotFoundException>(() => Container.Get<IInterface_1>(QueryMode.ThrowOnError));
            Assert.DoesNotThrow(() => child.Get<IInterface_1>(QueryMode.ThrowOnError));
        }


        [Test]
        public void IServiceContainer_CreateChildShouldNotTriggerTheTypeResolver()
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
        public void IServiceContainer_GetShuoldThrowOnNull() => Assert.Throws<ArgumentNullException>(() => Container.Get(null));

        [Test]
        public void IServiceContainer_AddShuoldThrowOnNull() => Assert.Throws<ArgumentNullException>(() => Container.Add(null));
    }

    [TestFixture]
    public class IServiceContainerTests : ServiceContainerTestsBase<ServiceContainer>
    {
    }

    //
    // 1) Ne generikus alatt legyen nested-kent (mert akkor valojaban "MyList<TParent, T>" a definicio).
    // 2) Azert kell leszarmazni h pontosan egy konstruktorunk legyen
    //

    public class MyList<T> : List<T> 
    {
        public MyList()
        {
        }
    }
}
