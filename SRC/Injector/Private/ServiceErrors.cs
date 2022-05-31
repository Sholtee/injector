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
        public static void NotFound(Type iface, string? name, AbstractServiceEntry? requestor)
        {
            MissingServiceEntry requested = new(iface, name);

            ServiceNotFoundException ex = new(string.Format(Culture, SERVICE_NOT_FOUND, requested.ToString(shortForm: true)));

            ex.Data[nameof(requested)] = requested;
            if (requestor is not null)
                ex.Data[nameof(requestor)] = requestor;

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
