/********************************************************************************
* ScopeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

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
        /// Creates a new scope factory. The returned instance defines the root scope too.
        /// </summary>
        /// <param name="registerServices">The callback to register services.</param>
        /// <param name="scopeOptions">The <see cref="ScopeOptions"/> to be applied against all the created scopes.</param>
        /// <param name="tag">Optional user defined data to be bound to the root.</param>
        public static IScopeFactory Create(Action<IServiceCollection> registerServices, ScopeOptions? scopeOptions = null, object? tag = null)
        {
            if (registerServices is null)
                throw new ArgumentNullException(nameof(registerServices));

            IServiceCollection services = ServiceCollection.Create();
            registerServices(services);

            return Create(services, scopeOptions, tag);
        }

        /// <summary>
        /// Creates a new scope factory. The returned instance defines the root scope too.
        /// </summary>
        /// <param name="services">Register services.</param>
        /// <param name="options">The <see cref="ScopeOptions"/> to be applied against all the created scopes.</param>
        /// <param name="tag">Optional user defined data to be bound to the root.</param>
        public static IScopeFactory Create(IServiceCollection services, ScopeOptions? options = null, object? tag = null)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            options ??= ScopeOptions.Default;

            return options.SupportsServiceProvider
                ? new InjectorSupportsServiceProvider(services, options, tag)
                : new Injector(services, options, tag);
        }
    }
}
