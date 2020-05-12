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
    internal static class Cache 
    {
        private static class Backend<TKey, TValue> 
        {
            public static ConcurrentDictionary<(TKey Key, string Scope), TValue> Value { get; } = new ConcurrentDictionary<(TKey Key, string Scope), TValue>();     
        }

        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TValue> factory, [CallerMemberName] string scope = "") => 
            Backend<TKey, TValue>.Value.GetOrAdd((key, scope), @void => factory());
    }
}
