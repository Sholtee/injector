/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    internal static class ServiceEntryExtensions
    {
        public static AbstractServiceEntry Specialize(this AbstractServiceEntry entry, params Type[] genericArguments)
        {
            Debug.Assert(entry.Implementation != null, "Attempt to specialize an entry without implementation");

            return ProducibleServiceEntry.Create
            (
                entry.Lifetime,
                entry.Interface.MakeGenericType(genericArguments), 
                entry.Name,
                entry.Implementation.MakeGenericType(genericArguments), 
                entry.Owner
            );
        }

        public static bool IsGeneric(this AbstractServiceEntry entry) => entry.Interface.IsGenericTypeDefinition();
    }
}
