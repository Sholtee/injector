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
    [SuppressMessage("Usage", "CA2217:Do not mark enums with FlagsAttribute"), SuppressMessage("Design", "CA1008:Enums should have zero value")]
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
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
        /// Indicates that the entry was already instantiated one or more times.
        /// </summary>
        Instantiated = Validated | 1 << 1, // TODO: remove

        /// <summary>
        /// Signals that no build phase required. It also means that the <see cref="AbstractServiceEntry.GetSingleInstance"/> method can be called.
        /// </summary>
        Built = Instantiated | 1 << 2, // TODO: remove

        /// <summary>
        /// A single instance will be created.
        /// </summary>
        CreateSingleInstance = 1 << 3,

        /// <summary>
        /// The created service instance is shared between scopes.
        /// </summary>
        Shared = 1 << 4,
    }
}
