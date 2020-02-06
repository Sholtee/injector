/********************************************************************************
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
    using Properties;

    public abstract class ServiceContainerTestsBase<TImplementation>: TestBase<TImplementation> where TImplementation : IServiceContainer, new()
    {
        [Test]
        public void IServiceContainer_Dispose_ShouldDisposeOwnedEntriesOnly()
        {
            Disposable 
                owned    = new Disposable(),
                notOwned = new Disposable();

            IServiceContainer container = new TImplementation();

            container.Add(new InstanceServiceEntry(typeof(IDisposable), null, owned, releaseOnDispose: true, owner: container));
            container.Add(new InstanceServiceEntry(typeof(IDisposableEx), null, notOwned, releaseOnDispose: true, owner: new TImplementation()));

            Assert.That(container.Count, Is.EqualTo(2));

            container.Dispose();
            
            Assert.That(owned.Disposed);
            Assert.That(notOwned.Disposed, Is.False);
        }

        [Test]
        public void IServiceContainer_CreateChild_ShouldCopyTheEntriesFromItsParent()
        {
            //
            // MockBehavior ne legyen megadva h mikor a GC felszabaditja a mock entitast
            // akkor az ne hasaljon el azert mert a Dispose(bool)-ra (ami egyebkent vedett
            // tag) nem volt hivva a Setup().
            //

            Mock<AbstractServiceEntry> entry = new Mock<AbstractServiceEntry>(typeof(IDisposable) /*iface*/, null, Lifetime.Transient, new TImplementation());              
            entry.Setup(e => e.CopyTo(It.IsAny<IServiceContainer>())).Returns<IServiceContainer>(sc => null);

            using (IServiceContainer container = Container.Add(entry.Object).CreateChild())
            {
                entry.Verify(e => e.CopyTo(It.Is<IServiceContainer>(sc => sc == container)), Times.Once);
            }
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Get_ShouldReturnOnTypeMatch(string name)
        {          
            var entry = new TransientServiceEntry(typeof(IList<>), name, typeof(MyList<>), Container);
            Container.Add(entry);

            Assert.That(Container.Get(typeof(IList<>), name, QueryModes.ThrowOnError), Is.EqualTo(entry));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Get_ShouldReturnTheSpecializedEntry(string name)
        {
            //
            // Azert singleton h az owner kontener ne valtozzon.
            //

            Container.Add(new SingletonServiceEntry(typeof(IList<>), name, typeof(MyList<>), Container));

            Assert.That(Container.Count, Is.EqualTo(1));
            Assert.Throws<ServiceNotFoundException>(() => Container.Get(typeof(IList<int>), name, QueryModes.ThrowOnError));
            Assert.That(Container.Count, Is.EqualTo(1));
            Assert.That(Container.Get(typeof(IList<int>), name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), name, typeof(MyList<int>), Container)));
            Assert.That(Container.Count, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Get_ShouldReturnTheSpecializedInheritedEntry(string name)
        {
            Container.Add(new SingletonServiceEntry(typeof(IList<>), name, typeof(MyList<>), Container));

            using (IServiceContainer child = Container.CreateChild())
            {
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.Throws<ServiceNotFoundException>(() => child.Get(typeof(IList<int>), name, QueryModes.ThrowOnError));
                Assert.That(Container.Count, Is.EqualTo(1));
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.That(child.Get(typeof(IList<int>), name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), name, typeof(MyList<int>), Container)));
                Assert.That(Container.Count, Is.EqualTo(2));
                Assert.That(child.Count, Is.EqualTo(2));
            }

            Assert.That(Container.Get(typeof(IList<int>), name, QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), name, typeof(MyList<int>), Container)));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Get_ShouldReturnExistingEntriesOnly(string name)
        {
            Container.Add(new SingletonServiceEntry(typeof(IList<>), name, typeof(MyList<>), Container));

            Assert.IsNull(Container.Get(typeof(IList<int>)));
            Assert.Throws<ServiceNotFoundException>(() => Container.Get(typeof(IList<int>), name, QueryModes.ThrowOnError));
            Assert.That(Container.Get(typeof(IList<>), name, QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<>), name, typeof(MyList<>), Container)));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Contains_ShouldSearchByGetHashCode(string name)
        {
            AbstractServiceEntry 
                entry1 = new AbstractServiceEntry(typeof(IDisposable), name),
                entry2 = new AbstractServiceEntry(typeof(IDisposable), name);

            Container.Add(entry1);
            
            Assert.That(entry1, Is.EqualTo(entry2));
            Assert.True(Container.Contains(entry1));
            Assert.True(Container.Contains(entry2));
        }


        [Test]
        public void IServiceContainer_Enumerator_ShouldBeIndependent()
        {
            var entry = new AbstractServiceEntry(typeof(IDisposable), null);

            Container.Add(entry);

            using (IEnumerator<AbstractServiceEntry> enumerator = Container.GetEnumerator())
            {
                Container.Add(new AbstractServiceEntry(typeof(IList<>), null));
                Assert.That(enumerator.MoveNext);
                Assert.AreSame(enumerator.Current, entry);
                Assert.False(enumerator.MoveNext());
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void IServiceContainer_Dispose_ShouldFreeInstancesIfReleaseOnDisposeWasSetToTrue(bool releaseOnDispose)
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
        public void IServiceContainer_Dispose_ShouldDisposeChildContainerAndItsEntries()
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
        public void IServiceContainer_Dispose_ShouldDisposeSpecializedEntries()
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
        public void IServiceContainer_ChildShouldInheritEntriesFromTheParent()
        {
            IServiceContainer child = Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .CreateChild();

            Assert.DoesNotThrow(() => child.Get<IInterface_1>(QueryModes.ThrowOnError));
        }

        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void IServiceContainer_InheritanceShouldChangeTheOwnerOnly(Lifetime lifetime)
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
                info.Instance,
                info.Implementation
            }.GetHashCode();
        }

        [Test]
        public void IServiceContainer_ChildShouldInheritProxies()
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
        public void IServiceContainer_CreateChild_ShouldNotTriggerTheTypeResolver()
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
        public void IServiceContainer_Get_ShouldThrowOnNull() => Assert.Throws<ArgumentNullException>(() => Container.Get(null));

        [Test]
        public void IServiceContainer_Add_ShouldThrowOnNull() => Assert.Throws<ArgumentNullException>(() => Container.Add(null));

        public void IServiceContainer_Add_ShouldAcceptMoreThanOneNamedService()
        {
            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), "cica", typeof(Disposable), Container)));
            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), "kutya", typeof(Disposable), Container)));

            Assert.That(Container.Count, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Add_ShouldThrowOnAlreadyRegisteredService(string name)
        {
            Container.Add(new SingletonServiceEntry(typeof(IDisposable), name, typeof(Disposable), Container));

            Assert.Throws<ServiceAlreadyRegisteredException>(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), name, typeof(Disposable), Container)));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Add_ShouldOverwriteAbstractEntries(string name) 
        {
            Container.Add(new AbstractServiceEntry(typeof(IDisposable), name));

            Assert.DoesNotThrow(() => Container.Add(new SingletonServiceEntry(typeof(IDisposable), name, typeof(Disposable), Container)));
            Assert.That(Container.Get(typeof(IDisposable), name), Is.InstanceOf<SingletonServiceEntry>());
        }

        [Test]
        public void IServiceContainer_Get_ShouldReturnTheSpecializedEntryInMultithreadedEnvironment()
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
        public void IServiceContainer_Get_ShouldReturnTheSpecializedInheritedEntryInMultithreadedEnvironment()
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
        public void IServiceContainer_Get_ShouldThrowOnNonInterface() => Assert.Throws<ArgumentException>(() => Container.Get(typeof(object)));

        [Test]
        public void IServiceContainer_Add_ShouldDisposeAbstractEntryOnOverride()
        {
            var entry = new AbstractServiceEntry(typeof(IInterface_1), null, null, Container);

            Container.Add(entry);
            Container.Add(new InstanceServiceEntry(typeof(IInterface_1), null, new Implementation_1_No_Dep(), false, Container));

            Assert.That(entry.Disposed);
        }

        [Test]
        public void IServiceContainer_CreateChild_ShouldThrowIfChildCountReachedTheLimit()
        {
            Config.Value.CompositeMaxChildCount = 1;

            Assert.DoesNotThrow(() => Container.CreateChild());
            Assert.Throws<InvalidOperationException>(() => Container.CreateChild(), Resources.TOO_MANY_CHILDREN);
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
