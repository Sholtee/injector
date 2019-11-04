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
    
    public partial class ContainerTestsBase<TContainer>
    {
        [TestCase(null)]
        [TestCase("cica")]
        public void Container_Abstract_ShouldRegisterAnOverridableService(string name)
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Abstract<IInterface_2>(name);

            Assert.That(Container.Count, Is.EqualTo(2));

            using (IServiceContainer child = Container.CreateChild())
            {
                //
                // Meglevo nem absztrakt szervizeket nem irhatjuk felul.
                //

                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_1, Implementation_1>());
                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Abstract<IInterface_1>());

                //
                // Es az absztraktot is csak egyszer lehet felulirni.
                //

                Assert.DoesNotThrow(() => child.Service<IInterface_2, Implementation_2>(name));
                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_2, Implementation_2>(name));
            }
        }

        [Test]
        public void Container_Abstract_InheritanceTest()
        {
            Container
                .Service<IInterface_1, Implementation_1>()
                .Abstract<IInterface_2>();

            using (IServiceContainer child = Container.CreateChild())
            {
                using (IServiceContainer grandChild = child.CreateChild())
                {
                    Assert.DoesNotThrow(() => grandChild.Service<IInterface_2, Implementation_2>());
                    Assert.Throws<ServiceAlreadyRegisteredException>(() => grandChild.Service<IInterface_2, Implementation_2>());

                    //
                    // Az h a gyerekben felulirtuk nem szabad h hatassal legyen a szulore.
                    //

                    Assert.DoesNotThrow(() => child.Service<IInterface_2, Implementation_2>());
                    Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_2, Implementation_2>());
                }             
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
