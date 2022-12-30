/********************************************************************************
* ServiceEntryLookup_BTree.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

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
            FEntryLookup.Compiled = FGenericEntryLookup.Compiled = true;
        }
    }
}
