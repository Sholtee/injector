/********************************************************************************
* ServiceEntryFlags.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the actual state of an <see cref="AbstractServiceEntry"/>
    /// </summary>
    [Flags]
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
    [SuppressMessage("Design", "CA1008:Enums should have zero value")]
    public enum ServiceEntryFlags: int
    {
        /// <summary>
        /// The default state.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Indicates that the entry is validated so it can be instantiated.
        /// </summary>
        Validated = 1 << 0,

        /// <summary>
        /// A single instance will be created.
        /// </summary>
        CreateSingleInstance = 1 << 1,

        /// <summary>
        /// The created service instance is shared between scopes.
        /// </summary>
        Shared = 1 << 2,
    }
}
