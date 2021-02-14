/********************************************************************************
* ServiceReferenceExtensions.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class ServiceReferenceExtensions
    {
        public static void SetInstance(this IServiceReference svc)
        {
            bool succeeded = svc.RelatedServiceEntry.SetInstance(svc);

            //
            // Elmeletileg a SetInstance() csak akkor lehet hivva ha szukseges is letrehozni a
            // szervizpeldanyt.
            //

            Debug.Assert(succeeded, $"{nameof(SetInstance)}() failed");
        }
    }
}
