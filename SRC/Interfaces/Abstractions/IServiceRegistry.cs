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
    public interface IServiceRegistry: IDisposableEx, INotifyOnDispose
    {
        /// <summary>
        /// Returns the <see cref="AbstractServiceEntry"/> associated with given interface and name.
        /// </summary>
        AbstractServiceEntry? GetEntry(Type iface, string? name);

        /// <summary>
        /// Resolves a regular entry.
        /// </summary>
        AbstractServiceEntry ResolveRegularEntry(int slot, AbstractServiceEntry originalEntry);

        /// <summary>
        /// Resolves a generic entry.
        /// </summary>
        /// <returns></returns>
        AbstractServiceEntry ResolveGenericEntry(int slot, Type specializedInterface, AbstractServiceEntry originalEntry);

        /// <summary>
        /// Returns all the registered entries.
        /// </summary>
        IReadOnlyCollection<AbstractServiceEntry> RegisteredEntries { get; }

        /// <summary>
        /// The parent registry
        /// </summary>
        IServiceRegistry? Parent { get; }

        /// <summary>
        /// The derived registries.
        /// </summary>
        IReadOnlyCollection<IServiceRegistry> DerivedRegistries { get; }
    }
}
