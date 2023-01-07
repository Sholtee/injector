/********************************************************************************
* ScopeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
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
        /// <param name="options">The <see cref="ScopeOptions"/> to be applied against all the created scopes.</param>
        /// <param name="tag">Optional user defined data to be bound to the root.</param>
        public static IScopeFactory Create(Action<IServiceCollection> registerServices, ScopeOptions? options = null, object? tag = null)
        {
            if (registerServices is null)
                throw new ArgumentNullException(nameof(registerServices));

            ServiceCollection services = new();
            registerServices(services);

            return Create(services, options, tag);
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

            options ??= new();

            return options.SupportsServiceProvider
                ? new InjectorSupportsServiceProvider(services, options, tag)
                : new Injector(services, options, tag);
        }
    }
}
