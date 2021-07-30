/********************************************************************************
* PoolService.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;

    internal sealed class PoolService<TInterface> : ObjectPool<IServiceReference>, IPool<TInterface> where TInterface: class
    {
        public PoolService(IScopeFactory scopeFactory, int capacity, string? name) : base
        (
            capacity,

            //
            // Ez itt trukkos mert:
            //   1) "injector" by design nem szalbiztos viszont ez a metodus lehet hivva paralell
            //   2) Minden egyes legyartott elemnek sajat scope kell (az egyes elemek kulon szalakban lehetnek hasznalva)
            //   3) Letrehozaskor a mar meglevo grafot boviteni kell 
            //

            () =>
            {
                IInjector dedicatedInjector = scopeFactory.CreateScope();
                dedicatedInjector.Meta(PooledLifetime.POOL_SCOPE, true);

                //
                // A referenciat magat adjuk vissza, hogy azt fuggosegkent menteni lehessen a
                // hivo scope-jaban.
                //

                IServiceReference serviceReference = dedicatedInjector.GetReference(typeof(TInterface), name);
                return serviceReference;
            },

            suppressItemDispose: true
        ) {}

        public PoolItem<IServiceReference> Get() => GetItem(CheckoutPolicy.Block, default)!;
    }
}
