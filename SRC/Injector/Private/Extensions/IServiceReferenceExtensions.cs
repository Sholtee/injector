/********************************************************************************
* IServiceReferenceExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class IServiceReferenceExtensions // TODO:  torolni
    {
        public static void SetInstance(this IServiceReference self)
        {
            //
            // Elmeletileg a SetInstance() csak akkor lehet hivva ha szukseges is letrehozni a
            // szervizpeldanyt.
            //

            if (!self.RelatedServiceEntry.SetInstance(self))
                throw new InvalidOperationException(); // TODO: error message
        }
    }
}
