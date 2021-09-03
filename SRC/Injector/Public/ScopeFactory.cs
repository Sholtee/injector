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
        public static IScopeFactory Create(Action<IServiceCollection> registerServices, CancellationToken cancellation = default)
        {
            Ensure.Parameter.IsNotNull(registerServices, nameof(registerServices));

            ServiceCollection serviceCollection = new();
            registerServices(serviceCollection);

            return new Internals.ScopeFactory(serviceCollection, cancellation);
        }
    }
}
