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
        public ServiceProvider(IServiceContainer parent, IReadOnlyDictionary<string, object>? options) : base(parent, options)
            => this.Instance<IServiceProvider>(this);

        public override Injector CreateScope(IReadOnlyDictionary<string, object>? options) => new ServiceProvider((IServiceContainer) Parent!, options);

        public override Injector CreateScope(IServiceContainer parent, IReadOnlyDictionary<string, object>? options) => new ServiceProvider(parent, options);

        //
        // IInjector.Get() elvileg sose adhatna vissza NULL-t viszont h biztositsuk 
        // h a ServiceProvider konstruktor parameterek feloldasakor se dobjon kivetelt
        // ezert itt megengedjuk.
        //

        object IInjector.Get(Type iface, string? name) => TryGet(iface, name)!;

        object? IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}
