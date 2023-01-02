/********************************************************************************
* RecursiveDependencyGraphBuilder.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class RecursiveDependencyGraphBuilder: IGraphBuilder
    {
        private readonly ServicePath FPath;

        private readonly ScopeOptions FOptions;

        private readonly IFactoryVisitor[] FVisitors;

        private readonly IBuildContext FBuildContext;

        private readonly IServiceEntryLookup FLookup;

        public RecursiveDependencyGraphBuilder(IServiceEntryLookup lookup, IBuildContext buildContext, ScopeOptions options)
        {
            FOptions = options;
            FPath = new ServicePath();
            FLookup = lookup;
            FVisitors = new IFactoryVisitor[]
            {
                new MergeProxiesVisitor(),
                new ApplyLifetimeManagerVisitor(),
                new ServiceRequestReplacerVisitor(FLookup, FPath, options.SupportsServiceProvider)
            };
            FBuildContext = buildContext;
        }

        public void Build(AbstractServiceEntry requested)
        {
            Debug.Assert(!requested.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (FOptions.StrictDI)
            {
                AbstractServiceEntry? requestor = FPath.Last;
                
                //
                // At the root of the dependency graph this validation makes no sense.
                //

                if (requestor?.State.HasFlag(ServiceEntryStates.Validated) is false)
                    ServiceErrors.EnsureNotBreaksTheRuleOfStrictDI(requestor, requested, FOptions.SupportsServiceProvider);
            }

            if (!requested.Features.HasFlag(ServiceEntryFeatures.SupportsBuild) || requested.State.HasFlag(ServiceEntryStates.Built))
                return;

            //
            // Throws if the request is circular
            //

            FPath.Push(requested);
            try
            {
                requested.Build(FBuildContext, FVisitors);
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
