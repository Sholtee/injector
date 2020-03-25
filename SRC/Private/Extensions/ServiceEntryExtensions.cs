/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal static class ServiceEntryExtensions
    {
        public static AbstractServiceEntry Specialize(this AbstractServiceEntry entry, params Type[] genericArguments) => entry is ISupportsSpecialization generic
            ? generic.Specialize(genericArguments)
            : throw new NotSupportedException();

        public static bool IsGeneric(this AbstractServiceEntry entry) => entry.Interface.IsGenericTypeDefinition;
    }
}
