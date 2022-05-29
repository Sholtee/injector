/********************************************************************************
* ServiceErrors.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using static Interfaces.Properties.Resources;

    internal static class ServiceErrors
    {
        public static void NotFound(Type iface, string? name, ServicePath? path)
        {
            MissingServiceEntry requested = new(iface, name);

            ServiceNotFoundException ex = new(string.Format(Culture, SERVICE_NOT_FOUND, requested.ToString(shortForm: true)));

            ex.Data["requested"] = requested;
            ex.Data["requestor"] = path?.Count > 0 ? path[^1] : null;
            throw ex;
        }

        public static void AlreadyRegistered(AbstractServiceEntry entry)
        {
            InvalidOperationException ex = new(SERVICE_ALREADY_REGISTERED);
            ex.Data[nameof(entry)] = entry;
            throw ex;
        }
    }
}
