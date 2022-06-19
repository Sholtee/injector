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

    using static Properties.Resources;

    internal static class ServiceErrors
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotFound(Type iface, string? name, AbstractServiceEntry? requestor)
        {
            MissingServiceEntry requested = new(iface, name);

            ServiceNotFoundException ex = new(string.Format(Culture, SERVICE_NOT_FOUND, requested.ToString(shortForm: true)));

            ex.Data[nameof(requestor)] = requestor;
            ex.Data[nameof(requested)] = requested;

            throw ex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureNotBreaksTheRuleOfStrictDI(AbstractServiceEntry requestor, AbstractServiceEntry requested)
        {
            //
            // The requested service should not exist longer than its requestor.
            //

            if (!requestor.State.HasFlag(ServiceEntryStates.Validated) && requested.Lifetime?.CompareTo(requestor.Lifetime!) < 0)
            {
                RequestNotAllowedException ex = new(STRICT_DI);
                ex.Data[nameof(requestor)] = requestor;
                ex.Data[nameof(requested)] = requested;

                throw ex;
            }
        }
    }
}
