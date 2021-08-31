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
        /// Resolves a regular entry.
        /// </summary>
        AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry);

        /// <summary>
        /// Resolves a generic entry.
        /// </summary>
        /// <returns></returns>
        AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry);

        /// <summary>
        /// Returns all the registered entries.
        /// </summary>
        ICollection<AbstractServiceEntry> RegisteredEntries { get; }

        /// <summary>
        /// The parent registry
        /// </summary>
        new IServiceRegistry? Parent { get; }
    }
}
