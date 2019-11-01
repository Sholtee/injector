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
        private static readonly ConcurrentDictionary<(TKey Key, string Scope), TValue> FCache = new ConcurrentDictionary<(TKey Key, string Scope), TValue>();

        public static TValue GetOrAdd(TKey key, Func<TValue> factory, [CallerMemberName] string scope = "") => FCache.GetOrAdd((key, scope), @void => factory());
    }
}
