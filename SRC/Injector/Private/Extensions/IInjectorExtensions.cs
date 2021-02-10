/********************************************************************************
* IInjectorExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal static class IInjectorExtensions
    {
        private static readonly MethodInfo GenericCast = MethodInfoExtractor
            .Extract<IEnumerable>(e => e.Cast<object>())
            .GetGenericMethodDefinition();

        public static void RegisterServiceEnumerator(this IInjector injector)
        {
            injector
                .UnderlyingContainer
                .Factory(typeof(IEnumerable<>), EnumeratorFactory, Lifetime.Scoped);

            static IEnumerable EnumeratorFactory(IInjector injector, Type iface)
            {
                Assert(iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                //
                // Tenyleges szervizinterface megszerzese (IEnumerable<IService>-bol kell kicsomagolni).
                //

                iface = iface.GetGenericArguments().Single();

                IEnumerable services = injector.Select(iface);

                //
                // Az eredmenyt (ami IEnumerable) pedig IEnumerable<IService> formara kell hozni.
                //

                return (IEnumerable) Cache
                    .GetOrAdd(iface, () => GenericCast.MakeGenericMethod(iface))
                    .ToStaticDelegate()
                    .Invoke(new object[] { services });
            }
        }

        public static IEnumerable Select(this IInjector injector, Type serviceInterface)
        {
            Ensure.Parameter.IsInterface(serviceInterface, nameof(serviceInterface));

            foreach (string? svcName in GetNames())
            {
                yield return injector.Get(serviceInterface, svcName);
            }

            string?[] GetNames() 
            {
                Func<AbstractServiceEntry, bool> filter = entry => !entry.IsInternal();

                if (serviceInterface.IsGenericType)
                {
                    Type genericIface = serviceInterface.GetGenericTypeDefinition();
                    filter = filter.And(entry => entry.Interface == serviceInterface || entry.Interface == genericIface);
                }
                else 
                    filter = filter.And(entry => entry.Interface == serviceInterface);

                return injector
                    .UnderlyingContainer
                    .Where(filter)
                    .Select(entry => entry.Name)
                    .Distinct() // lezart generikus mellett szerepelhet annak nyitott parja is

                    //
                    // Mivel a GetEnumerator() lock-ol, viszont generikus bejegyzes lezarasahoz irni kell a kontenert
                    // ezert h ne keruljunk dead lock-ba eloszor lekerdezzuk a teljes listat [ToArray()].
                    //

                    .ToArray();
            }
        }
    }
}
