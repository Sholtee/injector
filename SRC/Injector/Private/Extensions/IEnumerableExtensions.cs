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
        public static int FirstIndexOf<T>(this IEnumerable<T> src, T value, IEqualityComparer<T> comparer) => src
            .Select((item, i) => new 
            { 
                Item = item, 
                Index = (int?) i
            })
            .Where(x => comparer.Equals(x.Item, value))
            .Select(x => x.Index)
            .FirstOrDefault() ?? -1;
    }
}
