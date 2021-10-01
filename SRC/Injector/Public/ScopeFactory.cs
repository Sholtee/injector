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

            ServiceCollection serviceCollection = new();
            registerServices(serviceCollection);

            if (options is null)
                options = new ScopeOptions();

            return options.SupportsServiceProvider
                ? new ConcurrentInjectorSupportsServiceProvider(serviceCollection, options, cancellation)
                : new ConcurrentInjector(serviceCollection, options, cancellation);
        }
    }
}
