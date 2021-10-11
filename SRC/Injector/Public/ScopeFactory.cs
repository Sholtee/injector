/********************************************************************************
* ScopeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    /// <summary>
    /// Exposes the underlying <see cref="IScopeFactory"/> implementation.
    /// </summary>
    public static class ScopeFactory
    {
        /// <summary>
        /// Creates a new scope factory.
        /// </summary>
        public static IScopeFactory Create(Action<IServiceCollection> registerServices, ScopeOptions? options = null, CancellationToken cancellation = default)
        {
            Ensure.Parameter.IsNotNull(registerServices, nameof(registerServices));

            ServiceCollection services = new();
            registerServices(services);

            return Create(services, options, cancellation);
        }

        /// <summary>
        /// Creates a new scope factory.
        /// </summary>
        public static IScopeFactory Create(IServiceCollection services, ScopeOptions? options = null, CancellationToken cancellation = default)
        {
            Ensure.Parameter.IsNotNull(services, nameof(services));

            options ??= new();

            return options.SupportsServiceProvider
                ? new ConcurrentInjectorSupportsServiceProvider(services, options, cancellation)
                : new ConcurrentInjector(services, options, cancellation);
        }
    }
}
