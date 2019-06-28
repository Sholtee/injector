/********************************************************************************
* ContainerEntryExtensions.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal static class ContainerEntryExtensions
    {
        public static Func<IInjector, Type, object> CreateFactory(this ContainerEntry entry) => Resolver.Get(entry.Implementation).ConvertToFactory();
        public static void SetFactory(this ContainerEntry entry) => entry.Factory = entry.CreateFactory();
    }
}
