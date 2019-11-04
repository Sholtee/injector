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
        //
        // Nem vagom miert de ha "(ITypeResolver Resolver, Type Interface)" formaban adom meg a kulcsot
        // akkor hiaba lesz az oljektum hash kodja azonos parametereknel ugyanaz (a gyorsitotar 
        // implementacioban scope-al egyutt is) a szotarba meg is ket kulon bejegyzeskent kerul be.
        //

        public static Lazy<Type> AsLazy(this ITypeResolver resolver, Type iface) => Cache<(int Resolver, Type Interface), Lazy<Type>>.GetOrAdd
        (
            key: (resolver.GetHashCode(), iface), 
            factory: () => new Lazy<Type>
            (
                () => resolver.Resolve(iface),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );
    }
}
