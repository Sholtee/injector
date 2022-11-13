/********************************************************************************
* DictionaryLookup.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class DictionaryLookup: ILookup<CompositeKey, AbstractServiceEntry>
    {
        private readonly Dictionary<CompositeKey, AbstractServiceEntry> FDictionary;

        private DictionaryLookup(Dictionary<CompositeKey, AbstractServiceEntry> dictionary) => FDictionary = dictionary;

        public DictionaryLookup() : this(new Dictionary<CompositeKey, AbstractServiceEntry>()) { }

        public ILookup<CompositeKey, AbstractServiceEntry> Add(CompositeKey key, AbstractServiceEntry data) => new DictionaryLookup
        (
            new Dictionary<CompositeKey, AbstractServiceEntry>(FDictionary)
            {
                {key, data}
            }
        );

        public bool TryAdd(CompositeKey key, AbstractServiceEntry data)
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

        public bool TryGet(CompositeKey key, out AbstractServiceEntry data) => FDictionary.TryGetValue(key, out data);
    }
}
