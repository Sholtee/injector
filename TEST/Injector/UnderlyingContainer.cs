/********************************************************************************
* UnderlyingContainer.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void UnderlyingContainer_ShouldServeTheInjector() 
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>();

            using (IInjector injector = Container.CreateInjector()) 
            {
                Assert.That(injector.UnderlyingContainer.Parent, Is.SameAs(Container));
                Assert.That(injector.Get<IInterface_1>(), Is.TypeOf<Implementation_1_No_Dep>());

                injector.UnderlyingContainer.Proxy<IInterface_1>((i, c) => new Implementation_1_Non_Interface_Dep(0));
                Assert.That(injector.Get<IInterface_1>(), Is.TypeOf<Implementation_1_Non_Interface_Dep>());
            }
        }
    }
}
