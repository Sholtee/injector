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
        /// Gets the service instance associated with the given type and (optional) key.
        /// </summary>
        /// <param name="type">The "id" of the service to be resolved.</param>
        /// <param name="key">The (optional) key of the service (usually a name).</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service or one or more dependencies could not be found.</exception>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The identifier won't confuse the users of the API.")]
        object Get(Type type, object? key = null);

        /// <summary>
        /// Tries to get the service instance associated with the given type and (optional) key.
        /// </summary>
        /// <param name="type">The "id" of the service to be resolved.</param>
        /// <param name="key">The (optional) key of the service (usually a name).</param>
        /// <returns>The requested service instance or NULL.</returns>
        object? TryGet(Type type, object? key = null);

        /// <summary>
        /// Describes the scope behavior.
        /// </summary>
        ScopeOptions Options { get; }
    }
}