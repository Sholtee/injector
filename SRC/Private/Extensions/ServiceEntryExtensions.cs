/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using Proxy;

    internal static class ServiceEntryExtensions
    {
        public static bool IsGeneric(this AbstractServiceEntry entry) => entry.Interface.IsGenericTypeDefinition;

        public static void ApplyAspects(this AbstractServiceEntry entry) 
        {
            if (!(entry is ISupportsProxying setter) || setter.Factory == null)
                //
                // Generikus szerviz, Abstract(), Instance() eseten a metodus nem ertelmezett.
                //

                throw new InvalidOperationException(Resources.CANT_PROXY);

            foreach (AspectAttribute aspect in entry.Interface.GetCustomAttributes<AspectAttribute>(inherit: true))
            {
                //
                // Proxy tipus letrehozasa (GetGeneratedProxyType() validal is).
                //

                Type proxyType = ProxyFactory.GetGeneratedProxyType(entry.Interface, aspect.GetInterceptor(entry.Interface));

                //
                // Bovitjuk a hivasi lancot a decorator-al.
                //

                Func<IInjector, Type, object> oldFactory = setter.Factory;

                setter.Factory = Decorator;

                object Decorator(IInjector injector, Type iface) 
                {
                    object instance = oldFactory(injector, iface);

                    return injector.Instantiate(proxyType, new Dictionary<string, object>
                    {
                        {"target", instance}
                    });
                }
            }
        }
    }
}
