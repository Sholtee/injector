/********************************************************************************
* CreateInjector.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Interfaces;
    using Properties;

    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_CreateInjector_ShouldBeNullChecked() 
        {
            Assert.Throws<ArgumentNullException>(() => IServiceContainerAdvancedExtensions.CreateInjector(null));
        }

        [Test]
        public void Container_CreateInjector_ShouldThrowOnNotOverriddenAbstractService()
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .Abstract<IInterface_2>();

            var ioEx = Assert.Throws<InvalidOperationException>(() => Container.CreateInjector(), Resources.INVALID_INJECTOR_ENTRY);

            Assert.That(ioEx.Data["entry"], Is.EqualTo(Container.Get<IInterface_2>()));
        }

        public static IEnumerable<Func<IServiceContainer, AbstractServiceEntry>> BadEntries // statikusnak kell lennie
        {
            get 
            {
                yield return container => new BadServiceEntry(typeof(IInterface_1), null, container);
                yield return container => new AbstractServiceEntry(typeof(IInterface_1), null, container);
            }
        }

        [TestCaseSource(nameof(BadEntries))]
        public void Container_CreateInjector_ShouldNotAddTheChildIfSomethingWentWrong(Func<IServiceContainer, AbstractServiceEntry> factory)
        {
            Container.Add(factory(Container));

            Assert.Throws<InvalidOperationException>(() => Container.CreateInjector());
            Assert.That(Container.Children.Count, Is.EqualTo(0));
        }
    }
}
