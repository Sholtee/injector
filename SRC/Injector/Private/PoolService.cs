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

    internal sealed class PoolService<TInterface> : ObjectPool<IServiceReference>, IPool<TInterface> where TInterface: class
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
                // TODO: Eldonteni h Injector-t v Provider-t kell letrehozni
                //

                CurrentInjector ??= declaringContainer.CreateInjector(); // a letrehozott injector elettartamat "declaringContainer" kezeli
                try
                {
                    //
                    // A referenciat magat adjuk vissza, hogy azt fuggosegkent menteni lehessen a
                    // hivo scope-jaban.
                    //

                    return CurrentInjector.GetReference(typeof(TInterface), factoryName);
                }
                finally
                {
                    CurrentInjector = null;
                }                 
            },

            suppressItemDispose: true
        ) {}

        public PoolItem<IServiceReference> Get(CheckoutPolicy checkoutPolicy) => Get(checkoutPolicy, default);
    }
}
