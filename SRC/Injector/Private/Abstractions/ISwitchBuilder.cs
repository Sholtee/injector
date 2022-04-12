/********************************************************************************
* ISwitchBuilder.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Primitives.Patterns;

    internal interface ISwitchBuilder<TKey, TValue>
    {
        Func<TKey, TValue?> Build(IEnumerable<KeyValuePair<TKey, TValue>> cases);

        public sealed class Default: Singleton<Default>, ISwitchBuilder<TKey, TValue>
        {
            public Func<TKey, TValue?> Build(IEnumerable<KeyValuePair<TKey, TValue>> cases)
            {
                Dictionary<TKey, TValue> dict = cases.ToDictionary(x => x.Key, x => x.Value);

                return key => dict.TryGetValue(key, out TValue? result) ? result : default(TValue);
            }
        }
    }
}
