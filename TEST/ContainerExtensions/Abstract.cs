/********************************************************************************
* Abstract.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Interfaces;

    public partial class ContainerTestsBase<TContainer>
    {
        [Test]
        public void Container_Abstract_ShouldBeNullChecked() 
        {
            Assert.Throws<ArgumentNullException>(() => IServiceContainerExtensions.Abstract(null, typeof(IDisposable)));
            Assert.Throws<ArgumentNullException>(() => Container.Abstract(null));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void Container_Abstract_ShouldRegisterAnOverridableService(string name)
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .Abstract<IInterface_2>(name);

            Assert.That(Container.Count, Is.EqualTo(2));

            using (IServiceContainer child = Container.CreateChild())
            {
                //
                // Meglevo nem absztrakt szervizeket nem irhatjuk felul.
                //

                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_1, Implementation_1_No_Dep>());
                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Abstract<IInterface_1>());

                //
                // Es az absztraktot is csak egyszer lehet felulirni.
                //

                Assert.DoesNotThrow(() => child.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(name));
                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(name));
            }
        }

        [Test]
        public void Container_Abstract_InheritanceTest()
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>()
                .Abstract<IInterface_2>();

            using (IServiceContainer child = Container.CreateChild())
            {
                using (IServiceContainer grandChild = child.CreateChild())
                {
                    Assert.DoesNotThrow(() => grandChild.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>());
                    Assert.Throws<ServiceAlreadyRegisteredException>(() => grandChild.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>());

                    //
                    // Az h a gyerekben felulirtuk nem szabad h hatassal legyen a szulore.
                    //

                    Assert.DoesNotThrow(() => child.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>());
                    Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>());
                }             
            }
        }
    }
}
