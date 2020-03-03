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

            foreach(IServiceContainer child in new[] { Container.CreateChild(), Container.CreateChild() }) // ket gyereket hozzunk letre
            {
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.Throws<ServiceNotFoundException>(() => child.Get(typeof(IList<int>), name, QueryModes.ThrowOnError));
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.That(child.Get(typeof(IList<int>), name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), name, typeof(MyList<int>), Container)));        
                Assert.That(child.Count, Is.EqualTo(2));
            }

            Assert.That(Container.Count, Is.EqualTo(2));
            Assert.That(Container.Get(typeof(IList<int>), name, QueryModes.ThrowOnError), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), name, typeof(MyList<int>), Container)));
            Assert.That(Container.CreateChild().Count, Is.EqualTo(2));
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

        private sealed class LockableSingletonServiceEntry : ProducibleServiceEntry
        {
            private readonly Type FImplementation;

            public readonly ManualResetEventSlim Lock = new ManualResetEventSlim(true);

            public LockableSingletonServiceEntry(Type @interface, Type implementation, IServiceContainer owner) : base(@interface, null, DI.Lifetime.Singleton, owner)
            {
                FImplementation = implementation;
            }

            public override Type Implementation 
            {
                get 
                {
                    Lock.Wait();
                    return FImplementation;
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
