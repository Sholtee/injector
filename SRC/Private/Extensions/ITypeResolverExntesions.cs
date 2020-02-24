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
        public static Lazy<Type> AsLazy(this ITypeResolver resolver, Type iface)
        {
            //
            // Gyorsitotarazunk h adott resolver peldany adott interface-el csak egyszer legyen hivva
            //

            return Cache.GetOrAdd((resolver, iface), () => new Lazy<Type>(Resolve, LazyThreadSafetyMode.ExecutionAndPublication));
        
            Type Resolve() 
            {
                Type implementation = resolver.Resolve(iface);

                Ensure.IsNotNull(implementation);
                Ensure.IsAssignable(iface, implementation);

                return implementation;
            }        
        }
    }
}
