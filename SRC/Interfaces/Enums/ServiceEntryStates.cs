/********************************************************************************
* ServiceEntryStates.cs                                                         *
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
    [SuppressMessage("Usage", "CA2217:Do not mark enums with FlagsAttribute", Justification = "The value of 1 is a valid flag value.")]
    public enum ServiceEntryStates
    {
        /// <summary>
        /// The default state.
        /// </summary>
        #pragma warning disable CA1008 // "Default" is the zero value
        Default = 0,
        #pragma warning restore CA1008

        /// <summary>
        /// Indicates that the entry was already instantiated one or more times.
        /// </summary>
        Instantiated = 1,

        /// <summary>
        /// Signals that no build phase required. It also means that the <see cref="AbstractServiceEntry.GetSingleInstance"/> method can be called.
        /// </summary>
        Built = Instantiated | 2
    }
}
