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
    using Properties;

    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_CreateInjector_ShouldBeNullChecked() 
        {
            Assert.Throws<ArgumentNullException>(() => IServiceContainerExtensions.CreateInjector(null));
        }

        [Test]
        public void Container_Container_CreateInjector_ShouldThrowOnNotOverrodeAbstractService()
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .Abstract<IInterface_2>();

            var ioEx = Assert.Throws<InvalidOperationException>(() => Container.CreateInjector(), Resources.INVALID_INJECTOR_ENTRY);
            var abstractEntries = (IReadOnlyList<Type>)ioEx.Data["abstractEntries"];
            Assert.That(abstractEntries.Single(), Is.EqualTo(typeof(IInterface_2)));
        }
    }
}
