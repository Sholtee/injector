/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Interfaces;
    using Internals;
    using Properties;

    public abstract partial class ServiceContainerTestsBase<TImplementation>
    {
        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Get_ShouldReturnOnTypeMatch(string name)
        {          
            var entry = new TransientServiceEntry(typeof(IList<>), name, typeof(MyList<>), Container, int.MaxValue);
            Container.Add(entry);

            Assert.That(Container.Get(typeof(IList<>), name, QueryModes.ThrowOnMissing), Is.EqualTo(entry));
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
            Assert.Throws<ServiceNotFoundException>(() => Container.Get(typeof(IList<int>), name, QueryModes.ThrowOnMissing));
            Assert.That(Container.Count, Is.EqualTo(1));
            Assert.That(Container.Get(typeof(IList<int>), name, QueryModes.AllowSpecialization | QueryModes.ThrowOnMissing), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), name, typeof(MyList<int>), Container)));
            Assert.That(Container.Count, Is.EqualTo(2));
        }

        [Test]
        public void IServiceContainer_Get_ShouldThrowIfEntryCanNotBeSpecialized([Values(true, false)] bool useChild) 
        {
            Container.Add(new AbstractServiceEntry(typeof(IList<>), null, Container));

            IServiceContainer container = useChild ? Container.CreateChild() : Container;

            Assert.Throws<NotSupportedException>(() => container.Get(typeof(IList<int>), null, QueryModes.AllowSpecialization | QueryModes.ThrowOnMissing), Resources.ENTRY_CANNOT_BE_SPECIALIZED);
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Get_ShouldReturnTheSpecializedInheritedEntry(string name)
        {
            Container.Add(new SingletonServiceEntry(typeof(IList<>), name, typeof(MyList<>), Container));

            foreach(IServiceContainer child in new[] { Container.CreateChild(), Container.CreateChild() }) // ket gyereket hozzunk letre
            {
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.Throws<ServiceNotFoundException>(() => child.Get(typeof(IList<int>), name, QueryModes.ThrowOnMissing));
                Assert.That(child.Count, Is.EqualTo(1));
                Assert.That(child.Get(typeof(IList<int>), name, QueryModes.AllowSpecialization | QueryModes.ThrowOnMissing), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), name, typeof(MyList<int>), Container)));        
                Assert.That(child.Count, Is.EqualTo(2));
            }

            Assert.That(Container.Count, Is.EqualTo(2));
            Assert.That(Container.Get(typeof(IList<int>), name, QueryModes.ThrowOnMissing), Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), name, typeof(MyList<int>), Container)));
            Assert.That(Container.CreateChild().Count, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Get_ShouldReturnExistingEntriesOnly(string name)
        {
            Container.Add(new SingletonServiceEntry(typeof(IList<>), name, typeof(MyList<>), Container));

            Assert.IsNull(Container.Get(typeof(IList<int>)));
            Assert.Throws<ServiceNotFoundException>(() => Container.Get(typeof(IList<int>), name, QueryModes.ThrowOnMissing));
            Assert.That(Container.Get(typeof(IList<>), name, QueryModes.ThrowOnMissing), Is.EqualTo(new SingletonServiceEntry(typeof(IList<>), name, typeof(MyList<>), Container)));
        }

        [Test]
        public void IServiceContainer_Get_ShouldThrowOnNull() => Assert.Throws<ArgumentNullException>(() => Container.Get(null));

        [Test]
        public void IServiceContainer_Get_ShouldReturnTheSpecializedEntryInMultithreadedEnvironment()
        {
            Container.Add(new SingletonServiceEntry(typeof(IList<>), null, typeof(MyList<>), Container));

            var @lock = new ManualResetEventSlim(true);

            Task<AbstractServiceEntry>
                t1 = Task.Run(() => 
                {
                    @lock.Wait();
                    return Container.Get(typeof(IList<int>), null, QueryModes.ThrowOnMissing | QueryModes.AllowSpecialization);
                }),
                t2 = Task.Run(() => 
                {
                    @lock.Wait();
                    return Container.Get(typeof(IList<int>), null, QueryModes.ThrowOnMissing | QueryModes.AllowSpecialization);
                });
           
            Thread.Sleep(10);

            @lock.Set();

            //
            // Megvarjuk mig lefutnak.
            //

            Task.WaitAll(t1, t2);

            Assert.AreSame(t1.Result, t2.Result);
            Assert.That(t1.Result, Is.EqualTo(new SingletonServiceEntry(typeof(IList<int>), null, typeof(MyList<int>), Container)));
        }

        [Test]
        public void IServiceContainer_Get_ShouldReturnTheSpecializedInheritedEntryInMultithreadedEnvironment()
        {
            Container.Add(new SingletonServiceEntry(typeof(IList<>), null, typeof(MyList<>), Container));
            Assert.That(Container.Count, Is.EqualTo(1));

            IServiceContainer
                child1 = Container.CreateChild(),
                child2 = Container.CreateChild();

            Assert.That(child1.Count, Is.EqualTo(1));
            Assert.That(child2.Count, Is.EqualTo(1));

            var @lock = new ManualResetEventSlim(true);

            Task<AbstractServiceEntry>
                t1 = Task.Run(() =>
                {
                    @lock.Wait();
                    return child1.Get(typeof(IList<int>), null, QueryModes.ThrowOnMissing | QueryModes.AllowSpecialization);
                }),
                t2 = Task.Run(() =>
                {
                    @lock.Wait();
                    return child2.Get(typeof(IList<int>), null, QueryModes.ThrowOnMissing | QueryModes.AllowSpecialization);
                });

            Thread.Sleep(10);

            @lock.Set();

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
    }
}
