/********************************************************************************
* ITypeResolverExntesions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    internal static class ITypeResolverExntesions
    {
        public static Lazy<Type> AsLazy(this ITypeResolver resolver, Type iface) => Cache<(ITypeResolver Resolver, Type Interface), Lazy<Type>>.GetOrAdd
        (
            key: (resolver, iface), 
            factory: () => new Lazy<Type>(() => resolver.Resolve(iface), LazyThreadSafetyMode.ExecutionAndPublication)
        );
    }
}
