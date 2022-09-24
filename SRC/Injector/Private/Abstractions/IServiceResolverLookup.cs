/********************************************************************************
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
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        ServiceResolver? Get(Type iface, string? name);

        int Slots { get; }
    }
}
