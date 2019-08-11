/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    internal static class ServiceEntryExtensions
    {
        public static ServiceEntry Specialize(this ServiceEntry entry, params Type[] genericArguments)
        {
            //
            // Azzal h csak egyszer gyartjuk le a peldanyt majd azt klonozzuk megusszuk a
            // folyamatos MakeGenericType() es a Resolver.Get() hivasokat.
            //

            return (ServiceEntry) Cache<int, ServiceEntry>
                .GetOrAdd
                (
                    GetKey(), 
                    () => (ServiceEntry) entry.GetType().CreateInstance
                    (
                        new[]
                        {
                            typeof(Type), // interface
                            typeof(Type) // implementation
                        },
                        entry.Interface.MakeGenericType(genericArguments),
                        entry.Implementation.MakeGenericType(genericArguments)
                    )
                )
                .Clone();

            int GetKey() => genericArguments
                .Select(ga => ga.GetHashCode())
                .Aggregate(entry.GetHashCode(), (accu, current) => new {accu, current}.GetHashCode());
        }
    }
}
