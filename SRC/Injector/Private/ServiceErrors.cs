/********************************************************************************
* ServiceErrors.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
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
            ServiceId requested = new(iface, name);
            throw new ServiceNotFoundException
            (
                string.Format(Culture, SERVICE_NOT_FOUND, requested.ToString()),
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
                try
                {
                    ex.Data[nameof(requestor)] = requestor;
                    ex.Data[nameof(requested)] = requested;
                }
                catch (ArgumentException)
                {
                    //
                    // .NET FW throws if value assigned to Exception.Data is not serializable
                    //

                    Debug.Assert(Environment.Version.Major == 4, "Only .NET FW should complain about serialization");

                    ex.Data[nameof(requestor)] = requestor.ToString(shortForm: false);
                    ex.Data[nameof(requested)] = requested.ToString(shortForm: false);
                }

                throw ex;
            }

            //
            // The requested service should not exist longer than its requestor.
            //

            if (requested.Lifetime?.CompareTo(requestor.Lifetime!) < 0)
            {
                RequestNotAllowedException ex = new(STRICT_DI_DEP);
                try
                {
                    ex.Data[nameof(requestor)] = requestor;
                    ex.Data[nameof(requested)] = requested;
                }
                catch (ArgumentException)
                {
                    //
                    // .NET FW throws if value assigned to Exception.Data is not serializable
                    //

                    Debug.Assert(Environment.Version.Major == 4, "Only .NET FW should complain about serialization");

                    ex.Data[nameof(requestor)] = requestor.ToString(shortForm: false);
                    ex.Data[nameof(requested)] = requested.ToString(shortForm: false);
                }
                throw ex;
            }
        }
    }
}
