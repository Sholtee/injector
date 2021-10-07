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
    public enum ServiceEntryStates: int
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
        Instantiated = Validated | 1 << 1,

        /// <summary>
        /// Signals that no build phase required. It also means that the <see cref="AbstractServiceEntry.GetSingleInstance"/> method can be called.
        /// </summary>
        Built = Instantiated | 1 << 2
    }
}
