/********************************************************************************
* IInjector.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Annotations;

    /// <summary>
    /// Provides the mechanism for injecting resources.
    /// </summary>
    public interface IInjector: IDisposable
    {
        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be a non-generic interface.</param>
        /// <param name="target">The (optional) target who requested the dependency.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service or one or more dependencies could not be found.</exception>
        object Get(Type iface, Type target = null);

        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <param name="class">The class to be instantiated.</param>
        /// <param name="explicitArgs">The explicit arguments (in the form of [parameter name - parameter value]). Explicit arguments won't be resolved by the injector.</param>
        /// <returns>The new instance.</returns>
        /// <remarks>The <paramref name="class"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>. Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as a normal dependency.</remarks>
        /// <exception cref="ServiceNotFoundException">One or more dependecies could not be found.</exception>
        object Instantiate(Type @class, IReadOnlyDictionary<string, object> explicitArgs = null);

        /// <summary>
        /// Gets the <see cref="Lifetime"/> of the given service (type).
        /// </summary>
        /// <param name="iface">>The "id" of the service.</param>
        /// <returns>The <see cref="Lifetime"/> of the service if it is producible, null otherwise.</returns>
        Lifetime? LifetimeOf(Type iface);

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