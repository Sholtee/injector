/********************************************************************************
* IServiceEntryLookup.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IServiceEntryLookup
    {
        /// <summary>
        /// Gets the service associated with the given interface and name.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        AbstractServiceEntry? Get(Type iface, string? name);

        /// <summary>
        /// Slots required to store scoped services.
        /// </summary>
        int Slots { get; }
    }
}
