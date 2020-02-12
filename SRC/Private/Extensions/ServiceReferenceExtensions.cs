/********************************************************************************
* ServiceReferenceExtensions.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal static class ServiceReferenceExtensions
    {
        public static bool SetInstance(this ServiceReference svc, IReadOnlyDictionary<string, object> options) => svc.RelatedServiceEntry.SetInstance(svc, options);
    }
}
