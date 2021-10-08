/********************************************************************************
* IServiceCollectionExtensions.cs                                               *
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

    internal static class IServiceCollectionExtensions
    {
        //
        // Mivel a generikus bejegyzesek lekerdezese lassu (2-3szorosa a regularis bejegyzesek lekerdezesi idejenek)
        // ezert az ismert generikusokat is hozzacsapjuk a regularis kifejezesekhez.
        //

        public static void RegisterKnownGenericServices(this IServiceCollection entries)
        {
            IReadOnlyDictionary<IServiceId, ISupportsSpecialization> generics = entries
                .Where(entry => entry.Interface.IsGenericTypeDefinition)
                .ToDictionary(entry => entry, entry => (ISupportsSpecialization) entry, ServiceIdComparer.Instance);

            //
            // ToArray() azert kell hogy masolaton dolgozzunk
            //

            foreach (AbstractServiceEntry entry in entries.ToArray())
            {
                RegisterKnownGenericServicesFrom(entry);

                void RegisterKnownGenericServicesFrom(AbstractServiceEntry entry)
                {
                    if (entry.Implementation is null)
                        return;

                    foreach (ParameterInfo parameter in entry.Implementation.GetApplicableConstructor().GetParameters())
                    {
                        Type parameterType = parameter.ParameterType;

                        if (!parameterType.IsInterface || !parameterType.IsConstructedGenericType || parameterType.ContainsGenericParameters)
                            continue;

                        string? serviceName = parameter.GetCustomAttribute<OptionsAttribute>()?.Name;

                        //
                        // Mivel a specializalas kibaszott idoigenyes (a factory generalas es az aspektusok miatt) ezert
                        // csak akkor csinaljuk ha van is ertelme.
                        //

                        if (entries.Contains(new DummyServiceEntry(parameterType, serviceName)))
                            continue;

                        //
                        // Ha nincs a lezart szerviznek generikus parja akkor egyszeruen ugrunk tovabb, ha kesobb nem Injector.TryGet() hivassal
                        // probaljuk a szervizt lekerdezni akkor ugy is kivetel lesz.
                        //

                        if (!generics.TryGetValue(new DummyServiceEntry(parameterType.GetGenericTypeDefinition(), serviceName), out ISupportsSpecialization generic))
                            continue;

                        AbstractServiceEntry specialized = generic.Specialize(null, parameterType.GenericTypeArguments);

                        //
                        // Az ujonan letrehozott bejegyzes szinten tartalmazhat ujonan lezart generikust [lasd RegisterKnownGenericServices_ShouldSupportNestedGenerics() teszt]
                        // ezert ot is fel kell dolgozzuk.
                        //

                        RegisterKnownGenericServicesFrom(specialized);

                        entries.Add(specialized);
                    }
                }
            }
        }
    }

}
