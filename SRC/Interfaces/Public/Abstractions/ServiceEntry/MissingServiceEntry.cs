﻿/********************************************************************************
* MissingServiceEntry.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes a missing service.
    /// </summary>
    /// <remarks>This entry cannot be instantiated.</remarks>
    public class MissingServiceEntry : AbstractServiceEntry
    {
        /// <summary>
        /// Creates a new <see cref="MissingServiceEntry"/> instance.
        /// </summary>
        public MissingServiceEntry(Type @interface, object? name) : base(@interface, name, null, null, null, null) { }
    }
}