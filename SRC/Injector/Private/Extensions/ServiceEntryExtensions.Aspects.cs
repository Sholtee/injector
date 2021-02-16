/********************************************************************************
* ServiceEntryExtensions.Aspects.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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

        private static async Task<Func<IInjector, Type, object, object>> BuildDelegate(Type iface, Type interceptor)
        {
            interceptor = await ProxyFactory.GenerateProxyTypeAsync(iface, interceptor);

            Func<IInjector, IReadOnlyDictionary<string, object?>, object> factory = Resolver.GetExtended(interceptor);

            return (IInjector injector, Type iface, object instance) => factory(injector, new Dictionary<string, object?>
            {
                //
                // TODO: Es mi van ha nem "target" a parameter neve???
                //

                {"target", instance}
            });
        }

        //
        // Tudom h az "iface"-rol lekerdezhetnem az aspektusokat, viszont akkor sokkal
        // nehezebb lenne tesztelni.
        //

        internal static async Task<Func<IInjector, Type, object, object>[]> GenerateProxyDelegates(Type iface, IEnumerable<AspectAttribute> aspects)
        {
            //
            // A visszaadott dekoratorok sorrendje megegyezik az aspektusok sorrendjevel:
            // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall?view=net-5.0#System_Threading_Tasks_Task_WhenAll_System_Threading_Tasks_Task
            //

            return await Task.WhenAll(aspects.Select(AspectToDelegate));

            async Task<Func<IInjector, Type, object, object>> AspectToDelegate(AspectAttribute aspect)
            {
                return aspect.Kind switch
                {
                    AspectKind.Service => await BuildDelegate(iface, aspect.GetInterceptorType(iface)),
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

            //
            // Mivel a proxy-k legeneralasa sokaig tarthat ezert ahelyett h megvarnank amig az osszes proxy osszeallitasra 
            // kerul elinditunk egy task-ot (ami az osszes generalast elvegzi), a bejegyzes factory-jat pedig egy 
            // placeholder-el irjuk felul ami a tenyleges dekoratorokat fogja hivni ha majd egyszer elerhetoek lesznek.
            //

            Task<Func<IInjector, Type, object, object>[]> realDelegates = GenerateProxyDelegates
            (
                entry.Interface,
                aspects
            );
          
            entry.ApplyProxy((IInjector injector, Type iface, object current) => realDelegates

                //
                // - Blokkolodik ha meg nincsenek kesz, egybol visszater kulonben
                // - Lehet olvasva parhuzamosan (https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1?view=net-5.0#thread-safety)
                //

                .Result
                .Aggregate(
                    current, 
                    (object current, Func<IInjector, Type, object, object> decorator) => decorator(injector, iface, current)));
        }

        public static void ApplyInterceptor(this AbstractServiceEntry entry, Type interceptor)
        {
            if (entry is not ISupportsProxying || entry.Factory == null)
                throw new InvalidOperationException(Resources.CANT_PROXY);

            Task<Func<IInjector, Type, object, object>> realDelegate = BuildDelegate
            (
                entry.Interface,
                interceptor
            );

            entry.ApplyProxy((IInjector injector, Type iface, object current) => realDelegate.Result.Invoke(injector, iface, current));
        }
    }
}
