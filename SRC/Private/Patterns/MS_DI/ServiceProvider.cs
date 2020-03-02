/********************************************************************************
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

        object IInjector.Get(Type iface, string name) => TryGet(iface, null);

        object IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}
