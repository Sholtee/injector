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
        public static AbstractServiceEntry Specialize(this AbstractServiceEntry entry, params Type[] genericArguments)
        {
            //
            // "Service(typeof(IGeneric<>), ...)" eseten az implementaciot konkretizaljuk.
            //

            if (entry.Implementation != null) return SpecializeBy
            (
                entry.Implementation.MakeGenericType(genericArguments)
            );

            //
            // "Factory(typeof(IGeneric<>), ...)" eseten az eredeti factory lesz hivva a 
            // konkretizalt interface-re.
            //

            if (entry.Factory != null) return SpecializeBy
            (
                entry.Factory
            );

            throw new NotSupportedException();

            AbstractServiceEntry SpecializeBy<TParam>(TParam param) => ProducibleServiceEntry.Create
            (
                entry.Lifetime,
                entry.Interface.MakeGenericType(genericArguments),
                entry.Name,
                param,
                entry.Owner
            );
        }

        public static bool IsGeneric(this AbstractServiceEntry entry) => entry.Interface.IsGenericTypeDefinition();
    }
}
