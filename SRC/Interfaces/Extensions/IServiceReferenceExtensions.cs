/********************************************************************************
* IServiceReferenceExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines some extensions for the <see cref="IServiceReference"/> interface.
    /// </summary>
    public static class IServiceReferenceExtensions
    {
        /// <summary>
        /// Shortcut for setting a new service instance.
        /// </summary>
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

        /// <summary>
        /// Shortcut for getting a nervice instance.
        /// </summary>
        public static object GetInstance(this IServiceReference svc)
        {
            if (svc is null)
                throw new ArgumentNullException(nameof(svc));

            return svc.RelatedServiceEntry.GetInstance(svc);
        }
    }
}
