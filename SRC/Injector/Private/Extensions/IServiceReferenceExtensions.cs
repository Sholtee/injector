/********************************************************************************
* IServiceReferenceExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class IServiceReferenceExtensions
    {
        public static void SetInstance(this IServiceReference svc)
        {
            if (svc is null)
                throw new ArgumentNullException(nameof(svc));

            //
            // Elmeletileg a SetInstance() csak akkor lehet hivva ha szukseges is letrehozni a
            // szervizpeldanyt.
            //

            if (!svc.RelatedServiceEntry.SetInstance(svc))
                throw new InvalidOperationException(); // TODO: error message
        }

        public static object GetInstance(this IServiceReference svc)
        {
            if (svc is null)
                throw new ArgumentNullException(nameof(svc));

            return svc.RelatedServiceEntry.GetInstance(svc);
        }
    }
}
