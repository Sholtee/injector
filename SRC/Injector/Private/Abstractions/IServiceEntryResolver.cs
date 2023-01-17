/********************************************************************************
* IServiceEntryResolver.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IServiceEntryResolver
    {
        /// <summary>
        /// Resolves the service associated with the given interface and name.
        /// </summary>
        AbstractServiceEntry? Resolve(Type iface, string? name);

        /// <summary>
        /// Resolves the services associated with the given interface regardless their name.
        /// </summary>
        IEnumerable<AbstractServiceEntry> ResolveMany(Type iface);

        /// <summary>
        /// Slots required to store scoped services.
        /// </summary>
        int Slots { get; }
    }
}
