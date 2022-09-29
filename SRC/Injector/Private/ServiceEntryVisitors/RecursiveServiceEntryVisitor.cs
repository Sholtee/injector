/********************************************************************************
* RecursiveServiceEntryVisitor.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class RecursiveServiceEntryVisitor : IServiceEntryVisitor
    {
        private readonly ServicePath FPath;

        private readonly ScopeOptions FOptions;

        private readonly ServiceRequestReplacer FReplacer;

        private readonly IDelegateCompiler FCompiler;

        public RecursiveServiceEntryVisitor(IServiceResolverLookup lookup, IDelegateCompiler compiler, ScopeOptions options)
        {
            FOptions = options;
            FPath = new ServicePath();
            FReplacer = new ServiceRequestReplacer(lookup, FPath, options.SupportsServiceProvider);
            FCompiler = compiler;
        }

        public void Visit(AbstractServiceEntry requested)
        {
            Debug.Assert(!requested.Interface.IsGenericTypeDefinition, "Generic entry cannot be visited");

            //
            // At the root of the dependency graph this validation makes no sense. This validation should run even if
            // the requested entry is already built.
            //

            if (FOptions.StrictDI && FPath.Last is not null)
                ServiceErrors.EnsureNotBreaksTheRuleOfStrictDI(FPath.Last, requested);

            if (!requested.Features.HasFlag(ServiceEntryFeatures.SupportsVisit) || requested.State.HasFlag(ServiceEntryStates.Built))
                return;

            //
            // Throws if the request is circular
            //

            FPath.Push(requested);
            try
            {
                requested.VisitFactory(FReplacer.VisitLambda, FCompiler);
            }
            finally
            {
                FPath.Pop();
            }

            //
            // No circular reference, no Strict DI violation... entry is validated
            //

            requested.SetValidated();
        }
    }
}
