﻿/********************************************************************************
* ServiceProvider.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal class ServiceProvider : Injector, IServiceProvider, IInjector
    {
        protected ServiceProvider(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, ServiceGraph graph) : base(parent, factoryOptions, graph) { }

        public ServiceProvider(IServiceContainer parent) : base(parent)
            => this.Instance<IServiceProvider>(this, releaseOnDispose: false);

        protected override Injector Spawn(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, ServiceGraph graph) =>
            new ServiceProvider(parent, factoryOptions, graph);

        //
        // IInjector.Get() elvileg sose adhatna vissza NULL-t viszont h biztositsuk 
        // h a ServiceProvider konstruktor parameterek feloldasakor se dobjon kivetelt
        // ezert itt megengedjuk -> disable CS8603
        //

        #pragma warning disable CS8603
        object IInjector.Get(Type iface, string? name) => TryGet(iface, name);
        #pragma warning restore CS8603

        object? IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}
