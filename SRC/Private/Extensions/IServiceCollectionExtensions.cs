/********************************************************************************
* IServiceCollectionExtensions.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static class IServiceCollectionExtensions
    {
        public static AbstractServiceEntry Get(this IServiceCollection src, Type iface) => src.TryGet(iface, out var result)
            ? result
            : throw new ServiceNotFoundException(iface);

        public static AbstractServiceEntry GetClosest(this IServiceCollection src, Type iface) => src.TryGetClosest(iface, out var result)
            ? result
            : throw new ServiceNotFoundException(iface);

        public static AbstractServiceEntry Query(this IServiceCollection src, Type iface)
        {
            AbstractServiceEntry entry = src.GetClosest(iface);

            //
            // 1. eset: Azt az entitast adjuk vissza amit kerestunk.
            //

            if (entry.Interface == iface) return entry;

            //
            // 2. eset: Egy generikus bejegyzes lezart parjat kerdezzuk le
            //

            if (entry.IsGeneric())
            {
                //
                // 2a eset: A bejegyzesnek van beallitott gyar fv-e (pl Factory<TCica>() hivas) akkor nincs dolgunk.
                //

                if (entry.Factory != null) return entry;

                try
                {
                    //
                    // 2b eset: Konkretizalni kell a bejegyzest. Megjegyzendo h mentjuk is az uj bejegyzest igy a legkozelebb
                    //          mar csak az elso esetig fusson a Query().
                    //

                    //
                    // Ha nem mi vagyunk a tulajdonosok akkor lekerdezzuk a tulajdonostol es masoljuk sajat magunkhoz
                    // (a tulajdonos nyilvan rogzitani fogja az uj bejegyzest magahoz is).
                    //

                    if (entry.Owner != src) return entry
                        .Owner
                        .Query(iface)
                        .CopyTo(src);

                    //
                    // Ha mi vagyunk a tulajdonosok akkor nekunk kell lezarni a bejegyzest.
                    //

                    src.Add(entry = entry.Specialize(iface.GetGenericArguments()));
                    return entry;
                }
                catch (ServiceAlreadyRegisteredException)
                {
                    //
                    // Parhuzamos esetben az Add() dobhat kivetelt (belsoleg a CopyTo() is az Add()-et hivja) amennyiben
                    // ket szal is hivja a Query()-t ugyanarra az interface-re. Ilyenkor visszaadjuk a mar regisztralt 
                    // peldanyt.
                    //

                    return src.Get(iface);
                }
            }

            Debug.Fail("Failed to query an existing entry");
            return null;
        }
    }
}