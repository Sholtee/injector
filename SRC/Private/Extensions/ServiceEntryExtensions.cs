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
        public static Func<IInjector, Type, object> CreateFactory(this ServiceEntry entry) => Resolver.Get(entry.Implementation).ConvertToFactory();

        public static void SetFactory(this ServiceEntry entry) => entry.Factory = entry.CreateFactory();

        public static ServiceEntry Specialize(this ServiceEntry entry, params Type[] genericArguments)
        {
            Debug.Assert(entry.Lifetime.HasValue);

            var specialied = new ServiceEntry(entry.Interface.MakeGenericType(genericArguments), entry.Lifetime.Value, entry.Implementation.MakeGenericType(genericArguments));
            specialied.SetFactory();

            return specialied;
        }
    }
}
