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

        private interface IInjectorGetter 
        {
            IInjector Injector { get; }
        }

        private class InjectorGetter : IInjectorGetter 
        {
            public InjectorGetter(IInjector injector) => Injector = injector;
            public IInjector Injector { get; }
        }

        [Test]
        public void Injector_ShouldPassItselfToItsFactories()
        {
            Container.Factory<IInjectorGetter>(i => new InjectorGetter(i));

            using (IInjector i = Container.CreateChild().CreateInjector())
            {
                Assert.AreSame(i, i.Get<IInjectorGetter>().Injector);
            }
        }
    }
}
