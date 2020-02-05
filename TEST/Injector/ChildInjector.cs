/********************************************************************************
* ChildInjector.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Injector_ShouldResolveItself()
        {
            using (IInjector injector = Container.CreateChild().CreateInjector())
            {
                Assert.AreSame(injector, injector.Get<IInjector>());
            }
        }

        [Test]
        public void Injector_ShouldPassItselfToItsFactories()
        {
            Container.Factory<IInterface_7<IInjector>>(i => new Implementation_7_TInterface_Dependant<IInjector>(i));

            using (IInjector i = Container.CreateChild().CreateInjector())
            {
                Assert.AreSame(i, i.Get<IInterface_7<IInjector>>().Interface);
            }
        }
    }
}
