/********************************************************************************
* ITypeResolverExntesions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Properties;

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

                if (implementation == null)
                    throw new InvalidOperationException(); // TODO: error message

                if (!iface.IsInterfaceOf(implementation))
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.NOT_ASSIGNABLE, iface, implementation));

                return implementation;
            }        
        }
    }
}
