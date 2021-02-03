/********************************************************************************
* PoolService.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed class PoolService<TInterface> : ObjectPool<TInterface>, IPool<TInterface>, IPool where TInterface: class
    {
        public PoolService(IInjector injector, int capacity, string factoryName) : base
        (
            capacity,
            () =>
            {
                //
                // Ez itt trukkos mert:
                //   1) "injector" by design nem szalbiztos viszont ez a metodus lehet hivva paralell
                //   2) Minden egyes legyartott elemnek sajat scope kell (h ok maguk szalbiztosak legyenek)
                //   3) Letrehozaskor a mar meglevo grafot boviteni kell 
                //

                lock (injector) // maximum csak "capacity"-szer lesz hivva -> a lock erdemben nem befolyasolja a teljesitmenyt
                {
                    return injector.Get<TInterface>(factoryName);
                }
            }
        ) {}

        public PoolItem<TInterface> Get(CheckoutPolicy checkoutPolicy) => Get(checkoutPolicy, default);

        object IPool.Get(CheckoutPolicy checkoutPolicy) => Get(checkoutPolicy);
    }
}
