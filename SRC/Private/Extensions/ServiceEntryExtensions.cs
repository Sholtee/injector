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
        public static Func<IInjector, Type, object> CreateFactory(this ServiceEntry entry) => Resolver.Get(entry.Implementation).ConvertToFactory();

        public static void SetFactory(this ServiceEntry entry) => entry.Factory = entry.CreateFactory();

        public static ServiceEntry Specialize(this ServiceEntry enty, params Type[] genericArguments)
        {
            var specialied = new ServiceEntry(enty.Interface.MakeGenericType(genericArguments), enty.Implementation.MakeGenericType(genericArguments), enty.Lifetime);
            specialied.SetFactory();

            return specialied;
        }
    }
}
