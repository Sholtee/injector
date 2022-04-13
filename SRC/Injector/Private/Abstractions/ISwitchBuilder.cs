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

    internal interface ISwitchBuilder<TValue>
    {
        Func<int, TValue?> Build(IEnumerable<KeyValuePair<int, TValue>> cases);

        public sealed class Default: Singleton<Default>, ISwitchBuilder<TValue>
        {
            public Func<int, TValue?> Build(IEnumerable<KeyValuePair<int, TValue>> cases)
            {
                //
                // Dictionary performs much better against int keys
                //

                Dictionary<int, TValue> dict = cases.ToDictionary(x => x.Key, x => x.Value);

                return key => dict.TryGetValue(key, out TValue? result) ? result : default(TValue);
            }
        }
    }
}
