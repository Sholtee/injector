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
        public static ServiceEntry Specialize(this ServiceEntry entry, params Type[] genericArguments) => (ServiceEntry) entry.GetType().CreateInstance
        (
            new []
            {
                typeof(Type), // interface
                typeof(Type)  // implementation
            }, 
            entry.Interface.MakeGenericType(genericArguments), 
            entry.Implementation.MakeGenericType(genericArguments)
        );
    }
}
