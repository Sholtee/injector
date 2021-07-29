/********************************************************************************
* IReadOnlyDictionaryExtensions.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal static class IReadOnlyDictionaryExtensions
    {
        public static T GetValueOrDefault<T>(this IReadOnlyDictionary<string, object> src, string key) => src.TryGetValue(key, out object val) && (val is T inst)
            ? inst
            : default!;
    }
}
