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

    //
    // This class is to use the same validation logic in JIT and AOT resolutins too
    //

    internal static class ServiceErrors
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotFound(Type iface, object? name, AbstractServiceEntry? requestor)
        {
            MissingServiceEntry requested = new(iface, name);
            throw new ServiceNotFoundException
            (
                string.Format(Culture, SERVICE_NOT_FOUND, requested.ToString(shortForm: true)),
                requestor,
                requested
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureNotBreaksTheRuleOfStrictDI(AbstractServiceEntry requestor, AbstractServiceEntry requested, bool supportServiceProvider)
        {
            if ((requested.Interface == typeof(IInjector) || (supportServiceProvider && requested.Interface == typeof(IServiceProvider))) && requested.Name is null)
            {
                RequestNotAllowedException ex = new(STRICT_DI_SCOPE);
                ex.Data[nameof(requestor)] = requestor;
                ex.Data[nameof(requested)] = requested;

                throw ex;
            }

            //
            // The requested service should not exist longer than its requestor.
            //

            if (requested.Lifetime?.CompareTo(requestor.Lifetime!) < 0)
            {
                RequestNotAllowedException ex = new(STRICT_DI_DEP);
                ex.Data[nameof(requestor)] = requestor;
                ex.Data[nameof(requested)] = requested;

                throw ex;
            }
        }
    }
}
