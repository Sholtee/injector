/********************************************************************************
* ServiceEntryLookup_BTree.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceEntryLookup_BTree : ServiceEntryLookupBase<BTreeLookup>
    {
        public ServiceEntryLookup_BTree
        (
            IEnumerable<AbstractServiceEntry> entries,
            IDelegateCompiler compiler,
            Func<IServiceEntryLookup, IBuildContext, IServiceEntryBuilder> entryBuilderFactory
        ) : base(entries, compiler, entryBuilderFactory)
        {
            //
            // Base already filled the lookups so it's time to compile
            //

            Debug.Assert(!entries.Any() || FEntryLookup.Count > 0 || FGenericEntryLookup.Count > 0, "Uninitialized lookup");

            FEntryLookup = FEntryLookup.Compile(compiler);
            FGenericEntryLookup = FGenericEntryLookup.Compile(compiler);
        }
    }
}
