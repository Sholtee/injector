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
        /// <param name="options">The <see cref="ScopeOptions"/> to be applied against all the created scopes.</param>
        /// <param name="lifetimne">Optional hint specifying the object that is responsible for releasing the root scope</param>
        public static IScopeFactory Create(Action<IServiceCollection> registerServices, ScopeOptions? options = null, object? lifetimne = null)
        {
            Ensure.Parameter.IsNotNull(registerServices, nameof(registerServices));

            ServiceCollection services = new();
            registerServices(services);

            return Create(services, options, lifetimne);
        }

        /// <summary>
        /// Creates a new scope factory. The returned instance defines the root scope too.
        /// </summary>
        /// <param name="services">Register services.</param>
        /// <param name="options">The <see cref="ScopeOptions"/> to be applied against all the created scopes.</param>
        /// <param name="lifetimne">Optional hint specifying the object that is responsible for releasing the root scope</param>
        public static IScopeFactory Create(IServiceCollection services, ScopeOptions? options = null, object? lifetimne = null)
        {
            Ensure.Parameter.IsNotNull(services, nameof(services));

            options ??= new();

            return options.SupportsServiceProvider
                ? new InjectorSupportsServiceProvider(services, options, lifetimne)
                : new Injector(services, options, lifetimne);
        }
    }
}
