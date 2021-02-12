/********************************************************************************
* ServiceProvider.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ServiceProvider : Injector, IServiceProvider, IInjector
    {
        protected ServiceProvider(IServiceContainer parent, ServiceProvider forkFrom) : base(parent, forkFrom) { }

        public ServiceProvider(IServiceContainer parent, IReadOnlyDictionary<string, object>? factoryOptions = null) : base(parent, factoryOptions)
            => this.Instance<IServiceProvider>(this);

        internal override Injector Fork(IServiceContainer parent) => new ServiceProvider(parent, this);

        //
        // IInjector.Get() elvileg sose adhatna vissza NULL-t viszont h biztositsuk 
        // h a ServiceProvider konstruktor parameterek feloldasakor se dobjon kivetelt
        // ezert itt megengedjuk.
        //

        object IInjector.Get(Type iface, string? name) => TryGet(iface, name)!;

        object? IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}
