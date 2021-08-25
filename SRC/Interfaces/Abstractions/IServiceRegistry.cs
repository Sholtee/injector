/********************************************************************************
* IServiceRegistry.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;

    /// <summary>
    /// Defines the layout of an abstract service registry.
    /// </summary>
    public interface IServiceRegistry : IComposite<IServiceRegistry>, INotifyOnDispose
    {
        /// <summary>
        /// Returns the <see cref="AbstractServiceEntry"/> associated with given interface and name.
        /// </summary>
        AbstractServiceEntry? GetEntry(Type iface, string? name);

        /// <summary>
        /// Returns all the registered entries.
        /// </summary>
        IReadOnlyList<AbstractServiceEntry> RegisteredEntries { get; }
    }
}
