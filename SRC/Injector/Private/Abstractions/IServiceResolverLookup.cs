﻿/********************************************************************************
* IServiceResolverLookup.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Internals
{
    internal interface IServiceResolverLookup
    {
        /// <summary>
        /// Gets the service associated with the given interface and name.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        ServiceResolver? Get(Type iface, string? name);

        /// <summary>
        /// Slots required to store scoped services.
        /// </summary>
        int Slots { get; }
    }
}
