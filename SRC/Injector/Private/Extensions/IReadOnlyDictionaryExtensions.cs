/********************************************************************************
* IReadOnlyDictionaryExtensions.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    internal static class IReadOnlyDictionaryExtensions
    {
        public static T GetValueOrDefault<T>(this IReadOnlyDictionary<string, object> src, string key) => src.TryGetValue(key, out object val) && (val is T inst)
            ? inst
            : default!;

        public static IReadOnlyDictionary<string, object> Extend(this IReadOnlyDictionary<string, object> src, string key, object value)
        {
            Dictionary<string, object> result = src.ToDictionary(x => x.Key, x => x.Value);

            //
            // Felulcsapja ha mar vt ilyen nevu elem.
            //

            result[key] = value;

            return result;
        }
    }
}
