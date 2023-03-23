/********************************************************************************
* ServiceEntryStates.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the actual state of an <see cref="AbstractServiceEntry"/>
    /// </summary>
    [Flags]
    public enum ServiceEntryStates : int
    {
        /// <summary>
        /// The default state.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Indicates that the entry has already been built (meaning that the <see cref="AbstractServiceEntry.CreateInstance"/> property is not null).
        /// </summary>
        Built = 1 << 0,

        /// <summary>
        /// Indicates that the entry is validated so it can be instantiated.
        /// </summary>
        /// <remarks>Validation includes circular dependency and strict DI violation checks</remarks>
        Validated = 1 << 1,

        /// <summary>
        /// Indicates that the service had already been instantiated.
        /// </summary>
        Instantiated = 1 << 2,

        /// <summary>
        /// The created instances are supposed to be disposed by the system.
        /// </summary>
        GarbageCollected = 1 << 3
    }
}
