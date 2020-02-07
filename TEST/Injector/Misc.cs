/********************************************************************************
* Misc.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Internals;

    [TestFixture]
    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Injector_Misc_RequestedServiceMayStoreTheItsDependencies(
            [Values(true, false)] bool useChildContainer,
            [Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime dependant,
            [Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton, null)] Lifetime? dependency)
        {
            Config.Value.StrictDI = false;

            if (dependency != null)
                Container.Service<IDisposableEx, Disposable>(dependency.Value);
            else
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
            [Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime) 
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
