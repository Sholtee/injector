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
        public ServiceProvider(IServiceContainer parent) : base(parent, QueryModes.Default)
            => this.Instance<IServiceProvider>(this, releaseOnDispose: false);

        public object GetService(Type serviceType) => Get(serviceType, null);
    }
}
