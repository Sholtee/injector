/********************************************************************************
* ServiceProvider.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal class ServiceProvider : Injector, IServiceProvider, IInjector
    {
        public ServiceProvider(IServiceContainer parent) : base(parent)
            => this.Instance<IServiceProvider>(this, releaseOnDispose: false);

        object IInjector.Get(Type iface, string name) => TryGet(iface, null);

        object IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}
