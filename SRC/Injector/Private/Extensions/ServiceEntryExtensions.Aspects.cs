/********************************************************************************
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
    using Interfaces.Properties;
    using Primitives;
    using Proxy;

    internal static partial class ServiceEntryExtensions
    {
        private static Func<IInjector, Type, object, object> BuildDelegate(Type iface, Type interceptor)
        {
            interceptor = ProxyFactory.GenerateProxyType(iface, interceptor);

            ConstructorInfo ctor = interceptor.GetApplicableConstructor();

            IReadOnlyList<ParameterInfo> paramz = ctor.GetParameters();

            if (paramz.Count is 1 && IsTargetParameter(paramz[0]))
            {
                Func<object?[], object> ctorFn = ctor.ToStaticDelegate();

                return (IInjector injector, Type iface, object instance) => ctorFn(new object[] { instance });
            }
            else
            {
                IReadOnlyList<ParameterInfo> targetParam = paramz.Where(IsTargetParameter).ToArray();

                if (targetParam.Count is not 1)
                    throw new InvalidOperationException(Properties.Resources.TARGET_PARAM_CANNOT_BE_DETERMINED);

                string targetName = targetParam[0].Name;

                Func<IInjector, IReadOnlyDictionary<string, object?>, object> factory = ServiceActivator.GetExtended(ctor);

                return (IInjector injector, Type iface, object instance) => factory(injector, new Dictionary<string, object?>
                {
                    {targetName, instance}
                });
            }

            bool IsTargetParameter(ParameterInfo param) => param.ParameterType == iface && param.GetCustomAttribute<OptionsAttribute>()?.Name is null;
        }

        //
        // Tudom h az "iface"-rol lekerdezhetnem az aspektusokat, viszont akkor sokkal
        // nehezebb lenne tesztelni.
        //

        internal static Func<IInjector, Type, object, object>[] GenerateProxyDelegates(Type iface, IEnumerable<AspectAttribute> aspects)
        {
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

            if (entry is not ISupportsProxying || entry.Factory is null)
                throw new InvalidOperationException(Resources.PROXYING_NOT_SUPPORTED);

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
            if (entry is not ISupportsProxying || entry.Factory is null)
                throw new InvalidOperationException(Resources.PROXYING_NOT_SUPPORTED);

            Func<IInjector, Type, object, object> realDelegate = BuildDelegate
            (
                entry.Interface,
                interceptor
            );

            entry.ApplyProxy(realDelegate);
        }
    }
}
