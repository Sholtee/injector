/********************************************************************************
* ServiceReferenceExtensions.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    internal static class ServiceReferenceExtensions
    {
        public static void SetInstance(this ServiceReference svc, IReadOnlyDictionary<string, object> options)
        {
            bool succeeded = svc.RelatedServiceEntry.SetInstance(svc, options);
            Debug.Assert(succeeded, $"{nameof(SetInstance)}() failed");
        }
    }
}
