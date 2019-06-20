/********************************************************************************
* FuncExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal static class FuncExtensions
    {
        public static Func<IInjector, Type, object> ConvertToFactory(this Func<IInjector, object> resolver) => (injector, type) => resolver(injector);
    }
}
