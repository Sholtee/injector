/********************************************************************************
* IInjector.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Provides the mechanism for injecting resources.
    /// </summary>
    public interface IInjector: IDisposable
    {
        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be a non-generic interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="target">The (optional) target who requested the dependency.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service or one or more dependencies could not be found.</exception>
        object Get(Type iface, string name = null, Type target = null);

        /// <summary>
        /// Gets the <see cref="Lifetime"/> of the given service (type).
        /// </summary>
        /// <param name="iface">>The "id" of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The <see cref="Lifetime"/> of the service if it is producible, null otherwise.</returns>
        Lifetime? LifetimeOf(Type iface, string name = null);

        /// <summary>
        /// The event fired before a service requested. It's useful when you want to resolve contextual dependencies (e.g. HTTP request) or return service mocks.
        /// </summary>
        event InjectorEventHandler<InjectorEventArg> OnServiceRequest;

        /// <summary>
        /// The event fired after a service requested.
        /// </summary>
        /// <remarks>The handler might specifiy a mock service to be returned.</remarks>
        event InjectorEventHandler<InjectorEventArg> OnServiceRequested;
    }
}