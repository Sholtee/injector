/********************************************************************************
* Misc.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    [TestFixture]
    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Injector_Misc_RequestedServiceMayStoreItsDependencies(
            [Values(true, false)] bool useChildContainer,
            [ValueSource(nameof(Lifetimes))] Lifetime dependant,
            [ValueSource(nameof(Lifetimes))] Lifetime dependency)
        {
            Config.Value.Injector.StrictDI = false;

            Container.Service<IDisposableEx, Disposable>(dependency);

            IServiceContainer sourceContainer = useChildContainer ? Container.CreateChild() : Container;

            sourceContainer.Service<IInterface_7<IDisposableEx>, Implementation_7_TInterface_Dependant<IDisposableEx>>(dependant);

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = sourceContainer.CreateInjector())
                {
                    IInterface_7<IDisposableEx> svc = injector.Get<IInterface_7<IDisposableEx>>();

                    Assert.That(svc.Interface.Disposed, Is.False);
                }
            }
        }

        [Test]
        public void Injector_Misc_RequestedServiceMayStoreItsDependencies(
            [Values(true, false)] bool useChildContainer,
            [ValueSource(nameof(Lifetimes))] Lifetime dependant)
        {
            Config.Value.Injector.StrictDI = false;

            Container.Instance<IDisposableEx>(new Disposable());

            IServiceContainer sourceContainer = useChildContainer ? Container.CreateChild() : Container;

            sourceContainer.Service<IInterface_7<IDisposableEx>, Implementation_7_TInterface_Dependant<IDisposableEx>>(dependant);

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = sourceContainer.CreateInjector())
                {
                    IInterface_7<IDisposableEx> svc = injector.Get<IInterface_7<IDisposableEx>>();

                    Assert.That(svc.Interface.Disposed, Is.False);
                }
            }
        }

        [Test]
        public void Injector_Misc_RequestedServiceMayStoreTheParentInjector(
            [Values(true, false)] bool useChildContainer, 
            [ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            IServiceContainer sourceContainer = useChildContainer ? Container.CreateChild() : Container;

            sourceContainer.Service<IInterface_7<IInjector>, Implementation_7_TInterface_Dependant<IInjector>>(lifetime);

            //
            // Ket kulonallo injectort hozzunk letre.
            //

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = sourceContainer.CreateInjector())
                {
                    IInterface_7<IInjector> svc = injector.Get<IInterface_7<IInjector>>();

                    //
                    // Mivel az IInjector nem IDisposableEx leszarmazott ezert ugy ellenorzom h dispose-olt e
                    // hogy meghivom rajt a Get()-et.
                    //

                    Assert.DoesNotThrow(() => svc.Interface.Get<IInjector>());
                }
            }
        }
    }
}
