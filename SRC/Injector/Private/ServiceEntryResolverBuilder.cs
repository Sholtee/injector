/********************************************************************************
* ServiceEntryResolverBuilder.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class ServiceEntryResolverBuilder
    {
        public static ServiceEntryResolver Build(IReadOnlyCollection<AbstractServiceEntry> entries, ScopeOptions scopeOptions)
        {
            BatchedDelegateCompiler delegateCompiler = new();
            delegateCompiler.BeginBatch();

            ServiceEntryResolver result = new(entries, delegateCompiler, scopeOptions);

            delegateCompiler.Compile();
            return result;
        }
    }
}
