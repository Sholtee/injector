/********************************************************************************
* ServiceProvider.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ServiceProvider : Injector, IServiceProvider, IInjector
    {
        public ServiceProvider(IServiceContainer parent) : base(parent)
            => this.Instance<IServiceProvider>(this);

        public override Injector CreateScope()
        {
            CheckNotDisposed();
            return new ServiceProvider((IServiceContainer) Parent!);
        }

        public override Injector CreateScope(IServiceContainer parent)
        {
            CheckNotDisposed();
            return new ServiceProvider(parent);
        }

        //
        // IInjector.Get() elvileg sose adhatna vissza NULL-t viszont h biztositsuk 
        // h a ServiceProvider konstruktor parameterek feloldasakor se dobjon kivetelt
        // ezert itt megengedjuk.
        //

        object IInjector.Get(Type iface, string? name) => TryGet(iface, name)!;

        object? IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}
