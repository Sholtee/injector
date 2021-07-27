/********************************************************************************
* IServiceContainer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;

    /// <summary>
    /// Provides the mechanism of storing service entries.
    /// </summary>
    /// <remarks>The implementations have to be thread safe.</remarks>
    public interface IServiceContainer : IComposite<IServiceContainer>, IEnumerable<AbstractServiceEntry>, IDisposableEx
    {
        /// <summary>
        /// Adds a new entry to the container overwriting the existing value (if it was abstract).
        /// </summary>
        /// <param name="entry">The entry to be added.</param>
        /// <exception cref="ServiceAlreadyRegisteredException">A service has already been registered with the given interface.</exception>
        void Add(AbstractServiceEntry entry);

        /// <summary>
        /// Gets the service entry associated with the given interface and (optional) name.
        /// </summary>
        /// <param name="serviceInterface">The service interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="mode">Options</param>
        /// <returns>The requested service entry.</returns>
        /// <exception cref="ServiceNotFoundException">If the <see cref="QueryModes.ThrowOnMissing"/> flag was set and the service could not be found.</exception>
        /// <exception cref="NotSupportedException">If the service could not be specialized.</exception>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The identifier won't confuse the users of the API.")]
        AbstractServiceEntry? Get(Type serviceInterface, string? name = null, QueryModes mode = QueryModes.Default);

        /// <summary>
        /// The number of entries in this container.
        /// </summary>
        int Count { get; }
    }
}