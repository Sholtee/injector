﻿/********************************************************************************
* InjectorSupportsServiceProvider.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class InjectorSupportsServiceProvider : Injector_New, IServiceProvider, IInjector
    {
        public InjectorSupportsServiceProvider(ScopeFactory parent) : base(parent) { }

        //
        // IInjector.Get() elvileg sose adhatna vissza NULL-t viszont h biztositsuk 
        // h a ServiceProvider konstruktor parameterek feloldasakor se dobjon kivetelt
        // ezert itt megengedjuk.
        //

        object IInjector.Get(Type iface, string? name) => TryGet(iface, name)!;

        object? IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}