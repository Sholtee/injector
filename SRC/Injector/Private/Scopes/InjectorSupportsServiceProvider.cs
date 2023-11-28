/********************************************************************************
* InjectorSupportsServiceProvider.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    //                                        !!!ATTENTION!!!
    //
    // This class is a critical component therefore every modification should be done carefully, with
    // performance in mind.
    // - NO System.Linq
    // - NO System.Reflection
    // - After ANY modifications, run the unit & performance tests to verify there is no regression
    //

    internal class InjectorSupportsServiceProvider : Injector, IServiceProvider
    {
        protected static new IServiceCollection RegisterBuiltInServices(IServiceCollection services) => Injector
            .RegisterBuiltInServices(services)
            .Factory(typeof(IServiceProvider), static (i, _) =>  i, Lifetime.Scoped, ServiceOptions.Default with { DisposalMode = ServiceDisposalMode.Suppress });

        public InjectorSupportsServiceProvider(IServiceCollection services, ScopeOptions options, object? tag) : base
        (
            new ServiceResolver
            (
                RegisterBuiltInServices(services),
                options
            ),
            options,
            tag
        ) {}

        public InjectorSupportsServiceProvider(InjectorSupportsServiceProvider super, object? tag) : base(super, tag)
        {
        }

        public override object Get(Type type, object? key) => TryGet(type, key)!;

        public object GetService(Type type) => TryGet(type, null)!;

        public override IInjector CreateScope(object? tag)
        {
            CheckNotDisposed();
            return new InjectorSupportsServiceProvider(this, tag);
        }
    }
}
