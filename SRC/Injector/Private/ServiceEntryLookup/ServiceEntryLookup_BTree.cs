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

    internal sealed class ServiceEntryLookup_BTree : ServiceEntryLookupBase<CompiledBTreeLookup>
    {
        public ServiceEntryLookup_BTree
        (
            IEnumerable<AbstractServiceEntry> entries,
            IDelegateCompiler compiler,
            Func<IServiceEntryLookup, IBuildContext, IGraphBuilder> graphBuilderFactory
        ) : base(entries, compiler, () => new CompiledBTreeLookup(compiler), graphBuilderFactory)
        {
            //
            // Base already filled the lookups so it's time to compile
            //

            if (entries.Any())
                Debug.Assert(FEntryLookup.Count > 0 || FGenericEntryLookup.Count > 0, "Uninitialized lookup");

            FEntryLookup.Compiled = FGenericEntryLookup.Compiled = true;
        }
    }
}
