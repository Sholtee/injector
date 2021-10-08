/********************************************************************************
* InjectorSupportsServiceProvider.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    //                                        !!!FIGYELEM!!!
    //
    // Ez az osztaly kozponti komponens, ezert minden modositast korultekintoen, a teljesitmenyt szem elott tartva
    // kell elvegezni:
    // - nincs Sysmte.Linq
    // - nincs System.Reflection
    // - mindig futtassuk a teljesitmeny teszteket (is) hogy a hatekonysag nem romlott e
    //

    internal class InjectorSupportsServiceProvider : Injector, IServiceProvider, IInjector
    {
        public InjectorSupportsServiceProvider(ConcurrentInjectorSupportsServiceProvider parent) : base(parent) { }

        //
        // IInjector.Get() elvileg sose adhatna vissza NULL-t viszont h biztositsuk 
        // h a ServiceProvider konstruktor parameterek feloldasakor se dobjon kivetelt
        // ezert itt megengedjuk.
        //

        object IInjector.Get(Type iface, string? name) => TryGet(iface, name)!;

        object? IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}
