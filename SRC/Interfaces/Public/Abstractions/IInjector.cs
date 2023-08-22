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
    public interface IInjector: IDisposableEx, IHasTag
    {
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
        /// Describes the scope behavior.
        /// </summary>
        ScopeOptions Options { get; }
    }
}