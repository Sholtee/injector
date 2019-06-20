/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;

namespace Solti.Utils.DI.Internals
{
    internal static class Cache<TKey, TValue>
    {
        private static readonly ConcurrentDictionary<TKey, TValue> FCache = new ConcurrentDictionary<TKey, TValue>();

        public static TValue GetOrAdd(TKey key, Func<TValue> factory) => FCache.GetOrAdd(key, @void => factory());
    }
}
