﻿/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal sealed class ServicePath: IServicePath
    {
        //
        // Eredetileg itt lancolt listat hasznaltam de mint kiderult az nem epp a leggyorsabb
        //

        private readonly List<AbstractServiceEntry> FPath = new(capacity: 10);

        public AbstractServiceEntry? Last => FPath.Count > 0
#if NETSTANDARD2_1_OR_GREATER
            ? FPath[^1]
#else
            ? FPath[FPath.Count - 1]
#endif
            : null;

        public AbstractServiceEntry? First => FPath.Count > 0
            ? FPath[0]
            : null;

        public void CheckNotCircular()
        {
            Ensure.IsNotNull(Last, nameof(Last));

            AbstractServiceEntry last = Last!;

            int firstIndex = 0;
            bool found = false;

            for(; firstIndex < FPath.Count; firstIndex++)
            {
                AbstractServiceEntry current = FPath[firstIndex];

                if (current.Interface == last.Interface && current.Name == last.Name)
                {
                    found = true;
                    break;
                }
            }

            Debug.Assert(found);

            //
            // Ha egynel tobbszor szerepel az aktualis szerviz az aktualis utvonalon akkor korkoros referenciank van.
            //

            if (firstIndex < FPath.Count - 1)
                throw new CircularReferenceException(string.Format
                (
                    Resources.Culture,
                    Resources.CIRCULAR_REFERENCE,

                    //
                    // Csak magat a kort adjuk vissza.
                    //

                    Format
                    (
                        FPath.Skip(firstIndex)
                    )
                ));
        }

        public void Push(AbstractServiceEntry entry)
        {
            Ensure.Parameter.IsNotNull(entry, nameof(entry));

            FPath.Add(entry);
        }

        public void Pop() => FPath.RemoveAt(FPath.Count - 1);

        public static string Format(IEnumerable<IServiceId> path) => string.Join(" -> ", path.Select(IServiceIdExtensions.FriendlyName));

        public IEnumerator<AbstractServiceEntry> GetEnumerator() => FPath.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
