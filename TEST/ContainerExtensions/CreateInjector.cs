/********************************************************************************
* CreateInjector.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Internals;
    using Properties;

    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_CreateInjector_ShouldBeNullChecked() 
        {
            Assert.Throws<ArgumentNullException>(() => IServiceContainerExtensions.CreateInjector(null));
        }

        [Test]
        public void Container_CreateInjector_ShouldThrowOnNotOverriddenAbstractService()
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .Abstract<IInterface_2>();

            var ioEx = Assert.Throws<InvalidOperationException>(() => Container.CreateInjector(), Resources.INVALID_INJECTOR_ENTRY);
            var abstractEntries = (IReadOnlyList<Type>) ioEx.Data["abstractEntries"];
            Assert.That(abstractEntries.Single(), Is.EqualTo(typeof(IInterface_2)));
        }

        public static IEnumerable<AbstractServiceEntry> BadEntries 
        {
            get 
            {
                yield return new BadServiceEntry(typeof(IInterface_1), null);
                yield return new AbstractServiceEntry(typeof(IInterface_1), null);
            }
        }

        [TestCaseSource(nameof(BadEntries))]
        public void Container_CreateInjector_ShouldNotAddTheChildIfSomethingWentWrong(AbstractServiceEntry badEntry)
        {
            Container.Add(badEntry);

            Assert.Throws<InvalidOperationException>(() => Container.CreateInjector());
            Assert.That(Container.Children.Count, Is.EqualTo(0));
        }
    }
}
