﻿/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using DI.Tests;
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

            container.Add(new InstanceServiceEntry(typeof(IDisposable), null, owned, releaseOnDispose: true, owner: container));
            container.Add(new InstanceServiceEntry(typeof(IDisposableEx), null, notOwned, releaseOnDispose: true, owner: null));

            Assert.That(container.Count, Is.EqualTo(2));

            container.Dispose();
            
            Assert.That(owned.Disposed);
            Assert.That(notOwned.Disposed, Is.False);
        }

        [Test]
        public void IServiceContainer_CreateChildShouldCopyTheEntriesFromItsParent()
        {
            //
            // MockBehavior ne legyen megadva h mikor a GC felszabaditja a mock entitast
            // akkor az ne hasaljon el azert mert a Dispose(bool)-ra (ami egyebkent vedett
            // tag) nem volt hivva a Setup().
            //

            Mock<AbstractServiceEntry> entry = new Mock<AbstractServiceEntry>(typeof(IDisposable) /*iface*/, null, Lifetime.Transient, new TImplementation());              
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
                new InstanceServiceEntry(typeof(IDisposable), null, new Disposable(), false, null)
            );

            Assert.Throws<ServiceAlreadyRegisteredException>(() => container.Add(new InstanceServiceEntry(typeof(IDisposable), null, new Disposable(), false, null)));

            Assert.Throws<ServiceAlreadyRegisteredException>(() => CreateContainer
            (
                new InstanceServiceEntry(typeof(IDisposable), null, new Disposable(), false, null),
                new InstanceServiceEntry(typeof(IDisposable), null, new Disposable(), false, null)
            ));
        }

        [Test]
        public void IServiceContainer_GetShouldReturnOnTypeMatch()
        {          
            IServiceContainer container = CreateContainer();
            var entry = new TransientServiceEntry(typeof(IList<>), null, typeof(MyList<>), container);
            container.Add(entry);

            Assert.That(container.Get(typeof(IList<>), null, QueryModes.ThrowOnError), Is.EqualTo(entry));
        }

        [Test]
        public void IServiceContainer_GetShouldReturnTheSpecializedEntry()
        {
            IServiceContainer container = CreateContainer();

            //
            // Azert singleton h az owner kontener ne valtozzon.
            //
            
            container.Add(new SingletonServiceEntry(typeof(IList<>), null, typeof(MyList<>), container));

            Assert.That(container.Count, Is.EqualTo(1));
            Assert.Throws<ServiceNotFoundException>(() => container.Get(typeof(IList<int>), null, QueryModes.ThrowOnError));
            Assert.That(container.Count, Is.EqualTo(1));
            Assert.That(container.Get(typeof(IList<int>), null, QueryModes.AllowSpecialization | QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), null, typeof(MyList<int>), container)));
            Assert.That(container.Count, Is.EqualTo(2));
        }

        [Test]
        public void IServiceContainer_GetShouldReturnTheSpecializedInheritedEntry()
        {
            IServiceContainer container = CreateContainer();
            container.Add(new SingletonServiceEntry(typeof(IList<>), null, typeof(MyList<>), container));

            using (IServiceContainer child = container.CreateChild())
            {
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.Throws<ServiceNotFoundException>(() => child.Get(typeof(IList<int>), null, QueryModes.ThrowOnError));
                Assert.That(container.Count, Is.EqualTo(1));
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.That(child.Get(typeof(IList<int>), null, QueryModes.AllowSpecialization | QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), null, typeof(MyList<int>), container)));
                Assert.That(container.Count, Is.EqualTo(2));
                Assert.That(child.Count, Is.EqualTo(2));
            }

            Assert.That(container.Get(typeof(IList<int>), null, QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), null, typeof(MyList<int>), container)));
        }

        [Test]
        public void IServiceContainer_GetShouldReturnExistingEntriesOnly()
        {
            IServiceContainer container = CreateContainer
            (
                new SingletonServiceEntry(typeof(IList<>), null, typeof(MyList<>), null) 
            );

            Assert.IsNull(container.Get(typeof(IList<int>)));
            Assert.Throws<ServiceNotFoundException>(() => container.Get(typeof(IList<int>), null, QueryModes.ThrowOnError));
            Assert.That(container.Get(typeof(IList<>), null, QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<>), null, typeof(MyList<>), null)));
        }

        [Test]
        public void IServiceContainer_ContainsShouldSearchByGetHashCode()
        {
            AbstractServiceEntry 
                entry1 = new AbstractServiceEntry(typeof(IDisposable), null),
                entry2 = new AbstractServiceEntry(typeof(IDisposable), null);

            IServiceContainer container = CreateContainer(entry1);
            
            Assert.That(entry1, Is.EqualTo(entry2));
            Assert.True(container.Contains(entry1));
            Assert.True(container.Contains(entry2));
        }


        [Test]
        public void IServiceContainer_EnumeratorShouldBeIndependent()
        {
            var entry = new AbstractServiceEntry(typeof(IDisposable), null);

            IServiceContainer container = CreateContainer(entry);

            using (IEnumerator<AbstractServiceEntry> enumerator = container.GetEnumerator())
            {
                container.Add(new AbstractServiceEntry(typeof(IList<>), null));
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
                    .Service<IInterface_1, Implementation_1_No_Dep>()
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

        private sealed class BadDisposable: Disposable
        {
            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged) throw new Exception();
            }
        }

        [Test]
        public void IServiceContainer_ChildShouldInheritTheParentEntries()
        {
            IServiceContainer child = Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .CreateChild();

            Assert.That(child.Get<IInterface_1>(QueryModes.ThrowOnError).Implementation, Is.EqualTo(typeof(Implementation_1_No_Dep)));
        }

        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void IServiceContainer_ChildEnriesShouldNotChangeByDefault(Lifetime lifetime)
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime)
                .Factory<IInterface_2>(i => new Implementation_2_IInterface_1_Dependant(i.Get<IInterface_1>()), lifetime)
                .Instance<IDisposable>(new Disposable());

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
                info.Value,
                info.Implementation
            }.GetHashCode();
        }

        [Test]
        public void IServiceContainer_ChildShouldInheritTheProxies()
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
        public void IServiceContainer_ChildShouldBeIndependentFromTheParent()
        {
            IServiceContainer child = Container
                .CreateChild()
                .Service<IInterface_1, Implementation_1_No_Dep>();

            Assert.Throws<ServiceNotFoundException>(() => Container.Get<IInterface_1>(QueryModes.ThrowOnError));
            Assert.DoesNotThrow(() => child.Get<IInterface_1>(QueryModes.ThrowOnError));
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
        public void IServiceContainer_GetShouldThrowOnNull() => Assert.Throws<ArgumentNullException>(() => Container.Get(null));

        [Test]
        public void IServiceContainer_AddShouldThrowOnNull() => Assert.Throws<ArgumentNullException>(() => Container.Add(null));

        [Test]
        public void IServiceContainer_AddShouldAcceptMoreThanOneNamedService()
        {
            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), "cica", typeof(Disposable), Container)));
            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), "kutya", typeof(Disposable), Container)));

            Assert.That(Container.Count, Is.EqualTo(2));
        }

        [Test]
        public void IServiceContainer_AddShouldThrowOnAlreadyRegisteredService()
        {
            Container.Add(new SingletonServiceEntry(typeof(IDisposable), null, typeof(Disposable), Container));
            Container.Add(new SingletonServiceEntry(typeof(IDisposable), "cica", typeof(Disposable), Container));

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), null, typeof(Disposable), Container)));
            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), "cica", typeof(Disposable), Container)));
        }

        [Test]
        public void IServiceContainer_AddShouldOverwriteAbstractEntries() 
        {
            Container.Add(new AbstractServiceEntry(typeof(IDisposable), null));
            Container.Add(new AbstractServiceEntry(typeof(IDisposable), "cica"));

            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), null, typeof(Disposable), Container)));
            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), "cica", typeof(Disposable), Container)));

            Assert.That(Container.Get(typeof(IDisposable)), Is.InstanceOf<SingletonServiceEntry>());
            Assert.That(Container.Get(typeof(IDisposable), "cica"), Is.InstanceOf<SingletonServiceEntry>());
        }

        [Test]
        public void IServiceContainer_GetShouldTakeNameIntoAccount() 
        {
            AbstractServiceEntry
                entryWithoutName = new AbstractServiceEntry(typeof(IDisposable), null),
                entryWithName    = new AbstractServiceEntry(typeof(IDisposable), "cica");

            Container.Add(entryWithName);
            Container.Add(entryWithoutName);

            Assert.AreSame(Container.Get(typeof(IDisposable)), entryWithoutName);
            Assert.AreSame(Container.Get(typeof(IDisposable), "cica"), entryWithName);
        }

        [Test]
        public void IServiceContainer_GetShouldReturnTheSpecializedEntryInMultithreadedEnvironment()
        {
            var entry = new LockableSingletonServiceEntry(typeof(IList<>), typeof(MyList<>), Container);

            Container.Add(entry);
            entry.Lock.Reset();

            Task<AbstractServiceEntry>
                t1 = Task.Run(() => Container.Get(typeof(IList<int>), null, QueryModes.ThrowOnError | QueryModes.AllowSpecialization)),
                t2 = Task.Run(() => Container.Get(typeof(IList<int>), null, QueryModes.ThrowOnError | QueryModes.AllowSpecialization));
           
            Thread.Sleep(10);

            //
            // Mindket szal a lock-nal varakozik.
            //

            entry.Lock.Set();

            //
            // Megvarjuk mig lefutnak.
            //

            Task.WaitAll(t1, t2);

            Assert.AreSame(t1.Result, t2.Result);
            Assert.That(t1.Result, Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), null, typeof(MyList<int>), Container)));
        }

        //
        // TODO: FIXME: Ez igy elegge az implementaciora tamaszkodik (arra alapozunk h a factory-t ugy is specializalas elott keri el a kontener)
        //

        private sealed class LockableSingletonServiceEntry : AbstractServiceEntry
        {
            public readonly ManualResetEventSlim Lock = new ManualResetEventSlim(true);

            public LockableSingletonServiceEntry(Type @interface, Type implementation, IServiceContainer owner) : base(@interface, null, DI.Lifetime.Singleton, owner)
            {
                Implementation = implementation;
            }

            public override Type Implementation { get; }

            public override Func<IInjector, Type, object> Factory
            {
                get
                {
                    Lock.Wait();
                    return null;   
                }
            }
        }

        [Test]
        public void IServiceContainer_GetShouldReturnTheSpecializedInheritedEntryInMultithreadedEnvironment()
        {
            var entry = new LockableSingletonServiceEntry(typeof(IList<>), typeof(MyList<>), Container);

            Container.Add(entry);
            Assert.That(Container.Count, Is.EqualTo(1));

            IServiceContainer
                child1 = Container.CreateChild(),
                child2 = Container.CreateChild();

            Assert.That(child1.Count, Is.EqualTo(1));
            Assert.That(child2.Count, Is.EqualTo(1));

            entry.Lock.Reset();

            Task<AbstractServiceEntry>
                t1 = Task.Run(() => child1.Get(typeof(IList<int>), null, QueryModes.ThrowOnError | QueryModes.AllowSpecialization)),
                t2 = Task.Run(() => child2.Get(typeof(IList<int>), null, QueryModes.ThrowOnError | QueryModes.AllowSpecialization));

            Thread.Sleep(10);

            //
            // Mindket szal a get_Factory()-nal varakozik.
            //

            entry.Lock.Set();

            //
            // Megvarjuk mig lefutnak.
            //

            Task.WaitAll(t1, t2);

            Assert.AreSame(t1.Result, t2.Result);
            Assert.That(t1.Result, Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), null, typeof(MyList<int>), Container)));

            Assert.That(child1.Count, Is.EqualTo(2));
            Assert.That(child2.Count, Is.EqualTo(2));

            Assert.That(Container.Count, Is.EqualTo(2));
        }

        [Test]
        public void IServiceContainer_GetShouldThrowOnNonInterface() => Assert.Throws<ArgumentException>(() => Container.Get(typeof(object)));

        [Test]
        public void IServiceContainer_AddShouldDisposeAbstractEntryOnOverride()
        {
            var entry = new AbstractServiceEntry(typeof(IInterface_1), null);

            Container.Add(entry);
            Container.Add(new InstanceServiceEntry(typeof(IInterface_1), null, new Implementation_1_No_Dep(), false, Container));

            Assert.That(entry.Disposed);
        }
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
    }
}
