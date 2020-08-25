/********************************************************************************
* IInjector.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;

    /// <summary>
    /// Provides the mechanism for injecting resources.
    /// </summary>
    public interface IInjector: IDisposableEx
    {
        /// <summary>
        /// Gets the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be a (non open generic) interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service or one or more dependencies could not be found.</exception>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The identifier won't confuse the users of the API.")]
        object Get(Type iface, string? name = null);

        /// <summary>
        /// Tries to get the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be a (non open generic) interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The requested service instance if the resolution was successful, null otherwise.</returns>
        object? TryGet(Type iface, string? name = null);

        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <param name="class">The class to be instantiated.</param>
        /// <param name="explicitArgs">The explicit arguments (in the form of [parameter name - parameter value]). Explicit arguments won't be resolved by the injector.</param>
        /// <returns>The new instance.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>The <paramref name="class"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>.</description></item>
        /// <item><description>Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as a normal dependency.</description></item>
        /// <item><description>The caller is responsible for freeing the returned instance.</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ServiceNotFoundException">One or more dependecies could not be found.</exception>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The identifier won't confuse the users of the API.")]
        object Instantiate(Type @class, IReadOnlyDictionary<string, object>? explicitArgs = null);

        /// <summary>
        /// Gets the <see cref="IServiceContainer"/> associated with the injector.
        /// </summary>
        /// <remarks>Every injector has its own service container that serves it on service request. This container is a direct descendant of the container from which the injector was created.</remarks>
        IServiceContainer UnderlyingContainer { get; }
    }
}