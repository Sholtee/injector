/********************************************************************************
* ServiceProvider.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal class ServiceProvider : Injector, IServiceProvider
    {
        public ServiceProvider(IServiceContainer parent) : base(parent)
            => this.Instance<IServiceProvider>(this, releaseOnDispose: false);

        public override object Get(Type iface, string name) => TryGet(iface, name);

        public object GetService(Type serviceType) => Get(serviceType, null);
    }
}
