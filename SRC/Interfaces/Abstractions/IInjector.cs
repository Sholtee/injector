/********************************************************************************
* IInjector.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;

    /// <summary>
    /// Provides the mechanism for injecting resources.
    /// </summary>
    /// <remarks>The implementation of this interface should not be thread safe.</remarks>
    public interface IInjector: IDisposableEx
    {
        /// <summary>
        /// Gets the <see cref="IServiceReference"/> associated with the given interface and (optional) name.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be an interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The resolved service reference.</returns>
        /// <exception cref="ServiceNotFoundException">The service or one or more dependencies could not be found.</exception>
        IServiceReference GetReference(Type iface, string? name = null);

        /// <summary>
        /// Tries to get the <see cref="IServiceReference"/> associated with the given interface and (optional) name.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be an interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The resolved service reference or NULL.</returns>
        IServiceReference? TryGetReference(Type iface, string? name = null);

        /// <summary>
        /// Gets the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be an interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service or one or more dependencies could not be found.</exception>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The identifier won't confuse the users of the API.")]
        object Get(Type iface, string? name = null);

        /// <summary>
        /// Tries to get the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be an interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The requested service instance or NULL.</returns>
        object? TryGet(Type iface, string? name = null);

        /// <summary>
        /// Gets the <see cref="IServiceContainer"/> associated with the injector.
        /// </summary>
        /// <remarks>Every injector has its own service container that serves it on service request. This container is a direct descendant of the container from which the injector was created.</remarks>
        IServiceContainer UnderlyingContainer { get; }
    }
}