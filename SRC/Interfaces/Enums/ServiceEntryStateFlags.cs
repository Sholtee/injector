/********************************************************************************
* ServiceEntryStateFlags.cs                                                     *
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
    public enum ServiceEntryStateFlags : int
    {
        /// <summary>
        /// The default state.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Indicates that the entry has already been built.
        /// </summary>
        Built = 1 << 0,

        /// <summary>
        /// Indicates that the entry is validated so it can be instantiated.
        /// </summary>
        Validated = 1 << 1
    }
}
