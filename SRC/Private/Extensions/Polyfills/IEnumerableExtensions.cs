/********************************************************************************
* IEnumerableExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    internal static class IEnumerableExtensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> src, params T[] elemz) => src.Concat(elemz);
    }
}
