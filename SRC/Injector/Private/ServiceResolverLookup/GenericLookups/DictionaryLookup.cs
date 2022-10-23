/********************************************************************************
* DictionaryLookup.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    internal sealed class DictionaryLookup<TData>: ILookup<TData, DictionaryLookup<TData>>
    {
        private readonly Dictionary<CompositeKey, TData> FDictionary;

        private DictionaryLookup(Dictionary<CompositeKey, TData> dictionary) => FDictionary = dictionary;

        public DictionaryLookup() : this(new Dictionary<CompositeKey, TData>()) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DictionaryLookup<TData> Add(CompositeKey key, TData data) => new
        (
            new Dictionary<CompositeKey, TData>(FDictionary)
            {
                {key, data}
            }
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(CompositeKey key, TData data)
        {
#if NETSTANDARD2_1_OR_GREATER
            return FDictionary.TryAdd(key, data);
#else
            try
            {
                FDictionary.Add(key, data);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(CompositeKey key, out TData data) => FDictionary.TryGetValue(key, out data);
    }
}
