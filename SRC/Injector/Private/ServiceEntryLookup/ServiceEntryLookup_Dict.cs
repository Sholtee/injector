/********************************************************************************
* ServiceEntryLookup_Dict.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceEntryLookup_Dict : ServiceEntryLookupBase<DictionaryLookup>
    {
        public ServiceEntryLookup_Dict
        (
            IEnumerable<AbstractServiceEntry> entries,
            IDelegateCompiler compiler,
            Func<IServiceEntryLookup, IBuildContext, IGraphBuilder> graphBuilderFactory
        ) : base (entries, compiler, static () => new DictionaryLookup(), graphBuilderFactory) {}
    }
}
