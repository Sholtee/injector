/********************************************************************************
* PoolService.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed class PoolService<TInterface> : ObjectPool<TInterface>, IPool<TInterface>, IPool where TInterface: class
    {
        [ThreadStatic]
        private static IInjector? CurrentInjector;

        public PoolService(IServiceContainer declaringContainer, int capacity, string factoryName) : base
        (
            capacity,

            //
            // Ez itt trukkos mert:
            //   1) "injector" by design nem szalbiztos viszont ez a metodus lehet hivva paralell
            //   2) Minden egyes legyartott elemnek sajat scope kell (h ok maguk szalbiztosak legyenek)
            //   3) Letrehozaskor a mar meglevo grafot boviteni kell 
            //

            () =>
            {
                //
                // A CurrentInjector-os varazslas azert kell mert ez a callback nem factory/constructor
                // hivasban kerul megszolitasra -> E nelkul korkoros referencia eseten itt mindig uj
                // fuggosegi grafot hoznank letre -> dead lock
                //

                CurrentInjector ??= declaringContainer.CreateInjector(); // a letrehozott injector elettartamat "declaringContainer" kezeli
                try
                {
                    return CurrentInjector.Get<TInterface>(factoryName);
                }
                finally
                {
                    CurrentInjector = null;
                }                 
            },

            suppressItemDispose: true
        ) {}

        public PoolItem<TInterface> Get(CheckoutPolicy checkoutPolicy) => Get(checkoutPolicy, default);

        object IPool.Get(CheckoutPolicy checkoutPolicy) => Get(checkoutPolicy);
    }
}
