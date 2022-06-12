/********************************************************************************
* ServiceErrors.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using static Interfaces.Properties.Resources;

    internal static class ServiceErrors
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotFound(Type iface, string? name, AbstractServiceEntry? requestor)
        {
            MissingServiceEntry requested = new(iface, name);

            ServiceNotFoundException ex = new(string.Format(Culture, SERVICE_NOT_FOUND, requested.ToString(shortForm: true)));

            ex.Data[nameof(requested)] = requested;
            ex.Data[nameof(requestor)] = requestor;

            throw ex;
        }
    }
}
