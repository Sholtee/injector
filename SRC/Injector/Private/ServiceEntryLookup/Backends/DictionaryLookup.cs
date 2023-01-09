/********************************************************************************
* DictionaryLookup.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class DictionaryLookup: ILookup<IServiceId, AbstractServiceEntry, DictionaryLookup>
    {
        private readonly Dictionary<IServiceId, AbstractServiceEntry> FDictionary;

        private DictionaryLookup(Dictionary<IServiceId, AbstractServiceEntry> dictionary) => FDictionary = dictionary;

        public DictionaryLookup() : this(new Dictionary<IServiceId, AbstractServiceEntry>(ServiceIdComparer.Instance)) { }

        public DictionaryLookup With(IServiceId key, AbstractServiceEntry data) => new
        (
            new Dictionary<IServiceId, AbstractServiceEntry>(FDictionary, FDictionary.Comparer)
            {
                {key, data}
            }
        );

        public bool TryAdd(IServiceId key, AbstractServiceEntry data)
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

        public bool TryGet(IServiceId key, out AbstractServiceEntry data) => FDictionary.TryGetValue(key, out data);

        public int Count => FDictionary.Count;
    }
}
