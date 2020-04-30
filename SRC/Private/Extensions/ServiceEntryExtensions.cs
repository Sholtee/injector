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

        public static void ApplyProxy(this AbstractServiceEntry entry, Func<IInjector, Type, object, object> decorator) 
        {
            if (!(entry is ISupportsProxying setter) || setter.Factory == null)
                //
                // Generikus szerviz, Abstract(), Instance() eseten a metodus nem ertelmezett.
                //

                throw new InvalidOperationException(Resources.CANT_PROXY);

            //
            // Bovitjuk a hivasi lancot a decorator-al.
            //

            Func<IInjector, Type, object> oldFactory = setter.Factory;

            setter.Factory = (injector, type) => decorator(injector, type, oldFactory(injector, type));
        }

        public static void ApplyProxy(this AbstractServiceEntry entry, Type proxyType)
        {
            //
            // Proxy tipus letrehozasa (GetGeneratedProxyType() validal is).
            //

            proxyType = ProxyFactory.GetGeneratedProxyType(entry.Interface, proxyType);

            entry.ApplyProxy((injector, iface, instance) => injector.Instantiate(proxyType, new Dictionary<string, object>
            {
                {"target", instance}
            }));
        }

        public static void ApplyAspects(this AbstractServiceEntry entry) 
        {
            foreach (AspectAttribute aspect in entry.Interface.GetCustomAttributes<AspectAttribute>(inherit: true))
            {
                switch (aspect.Kind) 
                {
                    case AspectKind.Service:
                        entry.ApplyProxy(aspect.GetInterceptor(entry.Interface));
                        break;
                    case AspectKind.Factory:
                        entry.ApplyProxy(aspect.GetInterceptor);
                        break;
                }                
            }
        }
    }
}
