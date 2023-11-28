/********************************************************************************
* IServiceResolver.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Describes the constract how to resolve services.
    /// </summary>
    /// <remarks>Implementations must be thread-safe.</remarks>
    internal interface IServiceResolver
    {
        /// <summary>
        /// Resolves the service associated with the given interface and name.
        /// </summary>
        AbstractServiceEntry? Resolve(Type type, object? key);

        /// <summary>
        /// Resolves the services associated with the given interface regardless their name.
        /// </summary>
        IReadOnlyCollection<AbstractServiceEntry>? ResolveMany(Type type);

        /// <summary>
        /// Slots required to store scoped services.
        /// </summary>
        int Slots { get; }

        /// <summary>
        /// The underlying service entry builder.
        /// </summary>
        IServiceEntryBuilder ServiceEntryBuilder { get; }
    }
}
