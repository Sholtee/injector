/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using DI.Tests;
    using Interfaces;

    public abstract partial class ServiceContainerTestsBase<TImplementation>: TestBase<TImplementation> where TImplementation : IServiceContainer, new()
    {
        [TestCase(null)]
        [TestCase("cica")]
        public void IServiceContainer_Contains_ShouldSearchByGetHashCode(string name)
        {
            AbstractServiceEntry 
                entry1 = new AbstractServiceEntry(typeof(IDisposable), name, Container),
                entry2 = new AbstractServiceEntry(typeof(IDisposable), name, Container);

            Container.Add(entry1);
            
            Assert.That(Container.Contains(entry1));
            Assert.That(Container.Contains(entry2));
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
