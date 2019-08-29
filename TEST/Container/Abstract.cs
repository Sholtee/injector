/********************************************************************************
* Abstract.cs                                                                   *
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

    [TestFixture]
    public sealed partial class ContainerTests
    {
        [Test]
        public void Container_Abstract_ShouldRegisterAnOverridableService()
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Abstract<IInterface_2>();

            Assert.That(Container.Entries.Count, Is.EqualTo(2));

            using (IServiceContainer child = Container.CreateChild())
            {
                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_1, Implementation_1>());
                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Abstract<IInterface_1>());

                Assert.DoesNotThrow(() => child.Service<IInterface_2, Implementation_2>());
                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_2, Implementation_2>());
            }
        }

        [Test]
        public void Container_CreateInjector_ShouldThrowOnNotOverrodeAbstractService()
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Abstract<IInterface_2>();

            var ioEx = Assert.Throws<InvalidOperationException>(() => Container.CreateInjector(), Resources.INVALID_INJECTOR_ENTRY);
            var abstractEntries = (IReadOnlyList<Type>) ioEx.Data["abstractEntries"];
            Assert.That(abstractEntries.Single(), Is.EqualTo(typeof(IInterface_2)));
        }
    }
}
