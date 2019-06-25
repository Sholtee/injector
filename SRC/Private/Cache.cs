/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    internal static class Cache<TKey, TValue>
    {
        private static readonly ConcurrentDictionary<object, TValue> FCache = new ConcurrentDictionary<object, TValue>();

        public static TValue GetOrAdd(TKey key, Func<TValue> factory, [CallerMemberName] string scope = "") => FCache.GetOrAdd(new {scope, key}, @void => factory());
    }
}
