/********************************************************************************
* ShallowServiceEntryVisitor.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ShallowServiceEntryVisitor: IServiceEntryVisitor
    {
        private readonly IDelegateCompiler FCompiler;

        public ShallowServiceEntryVisitor(IDelegateCompiler compiler) => FCompiler = compiler;

        public void Visit(AbstractServiceEntry entry)
        {
            Debug.Assert(!entry.Interface.IsGenericTypeDefinition, "Generic entry cannot be visited");

            if (entry.Features.HasFlag(ServiceEntryFeatures.SupportsVisit) && !entry.State.HasFlag(ServiceEntryStates.Built))
                entry.VisitFactory(static _ => _, FCompiler);
        }
    }
}
