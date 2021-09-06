﻿/********************************************************************************
* ServiceEntryExtensions.Aspects.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;
    using Proxy;

    internal static partial class ServiceEntryExtensions
    {
        private static void ApplyProxy(this AbstractServiceEntry entry, Func<IInjector, Type, object, object> decorator) 
        {
            ISupportsProxying setter = (ISupportsProxying) entry;

            //
            // Bovitjuk a hivasi lancot a decorator-al.
            //

            Func<IInjector, Type, object> oldFactory = setter.Factory!;

            setter.Factory = (injector, type) => decorator(injector, type, oldFactory(injector, type));
        }

        private static Func<IInjector, Type, object, object> BuildDelegate(Type iface, Type interceptor)
        {
            interceptor = ProxyFactory.GenerateProxyType(iface, interceptor);

            ConstructorInfo ctor = interceptor.GetApplicableConstructor();

            ParameterInfo[] compatibleParamz = ctor
                .GetParameters()
                .Where(para => para.ParameterType == iface)
                .ToArray();

            string targetName = compatibleParamz.Length == 1
                ? compatibleParamz[0].Name
                : "target";

            Func<IInjector, IReadOnlyDictionary<string, object?>, object> factory = ServiceActivator.GetExtended(ctor);

            return (IInjector injector, Type iface, object instance) => factory(injector, new Dictionary<string, object?>
            {
                {targetName, instance}
            });
        }

        //
        // Tudom h az "iface"-rol lekerdezhetnem az aspektusokat, viszont akkor sokkal
        // nehezebb lenne tesztelni.
        //

        internal static Func<IInjector, Type, object, object>[] GenerateProxyDelegates(Type iface, IEnumerable<AspectAttribute> aspects)
        {
            //
            // A visszaadott dekoratorok sorrendje megegyezik az aspektusok sorrendjevel:
            // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall?view=net-5.0#System_Threading_Tasks_Task_WhenAll_System_Threading_Tasks_Task
            //

            return aspects.Select(AspectToDelegate).ToArray();

            Func<IInjector, Type, object, object> AspectToDelegate(AspectAttribute aspect)
            {
                return aspect.Kind switch
                {
                    AspectKind.Service => BuildDelegate(iface, aspect.GetInterceptorType(iface)),
                    AspectKind.Factory => aspect.GetInterceptor,
                    _ => throw new NotSupportedException()
                };
            }
        }

        public static void ApplyAspects(this AbstractServiceEntry entry) 
        {
            AspectAttribute[] aspects = entry
                .Interface
                .GetCustomAttributes<AspectAttribute>(inherit: true)
                .ToArray();

            if (!aspects.Any())
                return;

            //
            // Ha van az interface-en aspektus de a bejegyzes nem teszi lehetove a hasznalatat akkor kivetel
            // (megjegyzes: nyilt generikusoknak, peldany bejegyzeseknek biztosan nincs Factory-ja).
            //

            if (entry is not ISupportsProxying || entry.Factory == null)
                throw new InvalidOperationException(Resources.CANT_PROXY);

            Func<IInjector, Type, object, object>[] decorators = GenerateProxyDelegates
            (
                entry.Interface,
                aspects
            );

            foreach (Func<IInjector, Type, object, object> decorator in decorators)
            {
                entry.ApplyProxy(decorator);
            }
        }

        public static void ApplyInterceptor(this AbstractServiceEntry entry, Type interceptor)
        {
            if (entry is not ISupportsProxying || entry.Factory == null)
                throw new InvalidOperationException(Resources.CANT_PROXY);

            Func<IInjector, Type, object, object> realDelegate = BuildDelegate
            (
                entry.Interface,
                interceptor
            );

            entry.ApplyProxy(realDelegate);
        }
    }
}
