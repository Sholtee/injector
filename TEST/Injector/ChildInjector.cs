/********************************************************************************
* ChildInjector.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void Injector_ChildShouldResolveItself()
        {
            IInjector injector = Container.CreateChild().CreateInjector();
            Assert.AreSame(injector, injector.Get<IInjector>());
        }

        [Test]
        public void Injector_ChildShouldPassItselfToItsFactories()
        {
            IInjector injector = null;

            Container.Factory<IInterface_1>(i =>
            {
                injector = i;
                return new Implementation_1();
            });

            using (IInjector i = Container.CreateChild().CreateInjector())
            {
                i.Get<IInterface_1>();
                Assert.AreSame(i, injector);
            }
        }
    }
}
