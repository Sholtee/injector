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

    internal class ServiceProvider : Injector, IServiceProvider, IInjector, IScopeFactory
    {
        public ServiceProvider(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, IServiceGraph graph) : base(parent, factoryOptions, graph)
            => this.Instance<IServiceProvider>(this);

        public ServiceProvider(IServiceContainer parent, IReadOnlyDictionary<string, object>? factoryOptions = null) : base(parent, factoryOptions)
            => this.Instance<IServiceProvider>(this);

        IInjector IScopeFactory.CreateScope(IReadOnlyDictionary<string, object>? options) => new ServiceProvider(Parent!, options);

        IInjector IScopeFactory.CreateScope(IServiceContainer parent, IServiceGraph node, IReadOnlyDictionary<string, object>? options) => new ServiceProvider
        (
            Ensure.Parameter.IsNotNull(parent, nameof(parent)),
            options ?? new Dictionary<string, object>(0),
            Ensure.Parameter.IsNotNull(node, nameof(node))
        );

        //
        // IInjector.Get() elvileg sose adhatna vissza NULL-t viszont h biztositsuk 
        // h a ServiceProvider konstruktor parameterek feloldasakor se dobjon kivetelt
        // ezert itt megengedjuk.
        //

        object IInjector.Get(Type iface, string? name) => TryGet(iface, name)!;

        object? IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}
