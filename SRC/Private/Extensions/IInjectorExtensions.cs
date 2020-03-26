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
    internal static class IInjectorExtensions
    {
        private static readonly MethodInfo GenericCast = MethodInfoExtractor
            .Extract<IEnumerable>(e => e.Cast<object>())
            .GetGenericMethodDefinition();

        public static void RegisterSelf(this IInjector injector) => injector
            .UnderlyingContainer
            .Instance(injector, releaseOnDispose: false);

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
                    .Call(services);
            }
        }

        public static IEnumerable Select(this IInjector injector, Type serviceInterface)
        {
            Ensure.Parameter.IsInterface(serviceInterface, nameof(serviceInterface));

            IServiceContainer underlyingContainer = injector.UnderlyingContainer;

            //
            // Generikus interface eseten (ha kell) specializaljuk az osszes kompatibilis bejegyzest
            // az adott tipusra.
            //

            if (serviceInterface.IsGenericType)
            {
                Type genericIface = serviceInterface.GetGenericTypeDefinition();

                //
                // Mivel a GetEnumerator() lock-ol, viszont generikus bejegyzes lezarasahoz irni kell a kontenert
                // ezert h ne keruljunk dead lock-ba eloszor lekerdezzuk a teljes listat [ToArray()].
                //

                AbstractServiceEntry[] genericEntries = underlyingContainer
                    .Where(e => e.Interface == genericIface)
                    .ToArray();

                foreach (AbstractServiceEntry entry in genericEntries)
                {
                    Assert(entry.IsGeneric());

                    //
                    // Specializalas (ha mar le volt zarva akkor nem csinal semmit).
                    //

                    underlyingContainer.Get(serviceInterface, entry.Name, QueryModes.AllowSpecialization);
                }
            }

            //
            // Most mar biztosan minden kompatibilis bejegyzest vissza tudunk adni.
            //

            foreach (AbstractServiceEntry entry in underlyingContainer.Where(e => e.Interface == serviceInterface))
            {
                yield return injector.Get(serviceInterface, entry.Name);
            }
        }
    }
}
