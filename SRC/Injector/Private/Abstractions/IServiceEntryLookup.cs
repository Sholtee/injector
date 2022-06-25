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
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        AbstractServiceEntry? Get(Type iface, string? name);
    }
}
