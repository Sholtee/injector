/********************************************************************************
* ReadOnlyServiceCollection.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ReadOnlyServiceCollection: IReadOnlyServiceCollection
    {
        private readonly AbstractServiceEntry[] FEntries;

        public ReadOnlyServiceCollection(IEnumerable<AbstractServiceEntry> entries) => FEntries = entries.ToArray(); 

        public int Count => FEntries.Length;

        public IEnumerator<AbstractServiceEntry> GetEnumerator() => ((IEnumerable<AbstractServiceEntry>) FEntries).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FEntries.GetEnumerator();
    }
}
