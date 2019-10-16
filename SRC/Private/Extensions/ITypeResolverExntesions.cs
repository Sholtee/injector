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
        public static Lazy<Type> AsLazy(this ITypeResolver resolver, Type iface) => Cache<object, Lazy<Type>>.GetOrAdd
        (
            //
            // Nem vagom miert de ha "new {resolver, iface}" formaban adom meg a kulcsot
            // akkor hiaba lesz az anonim oljektum hash kodja azonos parametereknel ugyanaz
            // (a gyorsitotar implementacioban scope-al egyutt is) a szotarba meg is ket 
            // kulon bejegyzeskent kerul be.
            //

            key: new {hk = resolver.GetHashCode(), iface}, 
            factory: () => new Lazy<Type>
            (
                () => resolver.Resolve(iface),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );
    }
}
