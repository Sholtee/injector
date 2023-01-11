/********************************************************************************
* InjectorSupportsServiceProvider.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class InjectorSupportsServiceProvider : Injector, IServiceProvider
    {
        protected override void RegisterBuiltInServices(IServiceCollection services)
        {
            base.RegisterBuiltInServices(services);
            services.Factory<IServiceProvider>(static i => (IServiceProvider) i, Lifetime.Scoped);
        }

        public InjectorSupportsServiceProvider(IServiceCollection services, ScopeOptions options, object? tag) : base(services, options, tag)
        {
        }

        public InjectorSupportsServiceProvider(InjectorSupportsServiceProvider super, object? tag) : base(super, tag)
        {
        }

        public override object Get(Type iface, string? name) => TryGet(iface, name)!;

        public object GetService(Type serviceType) => TryGet(serviceType, null)!;

        public override IInjector CreateScope(object? tag) => new InjectorSupportsServiceProvider(this, tag);
    }
}
