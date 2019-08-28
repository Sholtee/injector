/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    internal static class ServiceEntryExtensions
    {
        public static ServiceEntry Specialize(this ServiceEntry entry, params Type[] genericArguments)
        {
            Debug.Assert(entry.Implementation != null, "Attempt to specialize an entry without implementation");

            //
            // Mivel a MakeGenericType() idoigenyes ezert gyorsitotarazunk.
            //

            var concrete = Cache<int, KeyValuePair<Type, Type>>.GetOrAdd
            (
                GetKey(), 
                () => new KeyValuePair<Type, Type>
                (
                    entry.Interface.MakeGenericType(genericArguments), 
                    entry.Implementation.MakeGenericType(genericArguments)
                )
            );

            return ProducibleServiceEntryFactory.CreateEntry(entry.Lifetime, concrete.Key, concrete.Value, entry.Owner);

            int GetKey() => genericArguments
                .Select(ga => ga.GetHashCode())
                .Aggregate(new {entry.Interface, entry.Implementation}.GetHashCode(), (accu, current) => new {accu, current}.GetHashCode());
        }

        public static bool IsGeneric(this ServiceEntry entry) => entry.Interface.IsGenericTypeDefinition();
    }
}
