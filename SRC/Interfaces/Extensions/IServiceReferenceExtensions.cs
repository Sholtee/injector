/********************************************************************************
* IServiceReferenceExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines some extensions to the <see cref="IServiceReference"/> interface.
    /// </summary>
    public static class IServiceReferenceExtensions
    {
        /// <summary>
        /// Gets the concrete value by applying the <see cref="AbstractServiceEntry.CustomConverters"/>.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static object GetEffectiveValue(this IServiceReference src) 
        {
            if (src is null)
                throw new ArgumentNullException(nameof(src));

            if (src.Value is null)
                throw new InvalidOperationException();

            return src
                .RelatedServiceEntry
                .CustomConverters
                .Aggregate(src.Value, (current, converter) => converter(current));
        }
    }
}
