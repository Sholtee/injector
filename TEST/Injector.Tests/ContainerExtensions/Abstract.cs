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
            Assert.Throws<ArgumentNullException>(() => IServiceContainerBasicExtensions.Abstract(null, typeof(IDisposable)));
            Assert.Throws<ArgumentNullException>(() => Container.Abstract(null));
        }

        [Test]
        public void Container_Abstract_ShouldRegisterAnOverridableService([Values(null, "cica")] string name, [ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Abstract<IInterface_2>(name);

            Assert.That(Container.Count, Is.EqualTo(2));

            using (IServiceContainer child = Container.CreateChild())
            {
                //
                // Meglevo nem absztrakt szervizeket nem irhatjuk felul.
                //

                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient));
                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Abstract<IInterface_1>());

                //
                // Es az absztraktot is csak egyszer lehet felulirni.
                //

                Assert.DoesNotThrow(() => child.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(name, lifetime));
                Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(name, Lifetime.Transient));
            }
        }

        [Test]
        public void Container_Abstract_InheritanceTest([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Container
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Abstract<IInterface_2>();

            using (IServiceContainer child = Container.CreateChild())
            {
                using (IServiceContainer grandChild = child.CreateChild())
                {
                    Assert.DoesNotThrow(() => grandChild.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(lifetime));
                    Assert.Throws<ServiceAlreadyRegisteredException>(() => grandChild.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Transient));

                    //
                    // Az h a gyerekben felulirtuk nem szabad h hatassal legyen a szulore.
                    //

                    Assert.DoesNotThrow(() => child.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(lifetime));
                    Assert.Throws<ServiceAlreadyRegisteredException>(() => child.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Transient));
                }             
            }
        }
    }
}
