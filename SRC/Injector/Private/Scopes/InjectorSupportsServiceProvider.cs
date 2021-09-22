/********************************************************************************
* InjectorSupportsServiceProvider.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class InjectorSupportsServiceProvider : Injector, IServiceProvider, IInjector
    {
        protected override Injector CreateDerived() => new InjectorSupportsServiceProvider(this);

        protected override IReadOnlyCollection<AbstractServiceEntry> BuiltInServices
        {
            get
            {
                List<AbstractServiceEntry> serviceList = new(base.BuiltInServices);
                serviceList.Add(new ContextualServiceEntry(typeof(IServiceProvider), null, owner => (IServiceProvider) owner));
                return serviceList;
            }
        }

        public InjectorSupportsServiceProvider(ISet<AbstractServiceEntry> entries, ScopeOptions options, CancellationToken cancellation) : base(entries, options, cancellation) { }

        public InjectorSupportsServiceProvider(InjectorSupportsServiceProvider parent) : base(parent) { }

        //
        // IInjector.Get() elvileg sose adhatna vissza NULL-t viszont h biztositsuk 
        // h a ServiceProvider konstruktor parameterek feloldasakor se dobjon kivetelt
        // ezert itt megengedjuk.
        //

        object IInjector.Get(Type iface, string? name) => TryGet(iface, name)!;

        object? IServiceProvider.GetService(Type serviceType) => TryGet(serviceType, null);
    }
}
