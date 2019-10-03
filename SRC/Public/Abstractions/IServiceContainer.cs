/********************************************************************************
* IServiceContainer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// Provides the mechanism of storing services.
    /// </summary>
    public interface IServiceContainer : IComposite<IServiceContainer>, IEnumerable<AbstractServiceEntry>
    {
        /// <summary>
        /// Adds a new entry to the container overwriting the existing value (if it was abstract).
        /// </summary>
        /// <param name="entry">The entry to be added.</param>
        /// <returns>The container itself.</returns>
        /// <exception cref="ServiceAlreadyRegisteredException">A service has already been registered with the given interface.</exception>
        IServiceContainer Add(AbstractServiceEntry entry);

        /// <summary>
        /// Gets the service associated with the given interface.
        /// </summary>
        /// <param name="serviceInterface">The service interface.</param>
        /// <param name="mode">Options</param>
        /// <returns>The requested service entry.</returns>
        /// <exception cref="ServiceNotFoundException">If the service could not be found.</exception>
        AbstractServiceEntry Get(Type serviceInterface, QueryMode mode = QueryMode.Default);

        /// <summary>
        /// The number of entries in this container.
        /// </summary>
        int Count { get; }
    }
}