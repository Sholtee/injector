/********************************************************************************
* ServiceEntryBuilderAot.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceEntryBuilderAot : ServiceEntryBuilder
    {
        private readonly ServicePath FPath;

        private readonly ScopeOptions FOptions;
#if DEBUG
        private readonly IServiceResolverLookup FLookup;
#else
        private readonly ServiceRequestReplacer FReplacer;
#endif
        public new const ServiceResolutionMode Id = ServiceResolutionMode.AOT;

        public ServiceEntryBuilderAot(IServiceResolverLookup lookup, ScopeOptions options)
        {
            FOptions = options;
            FPath = new ServicePath();
#if DEBUG
            FLookup = lookup;
#else
            FReplacer = new ServiceRequestReplacer(lookup, FPath, options.SupportsServiceProvider);
#endif
        }

        public override void Build(AbstractServiceEntry requested)
        {
            Debug.Assert(!requested.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            //
            // At the root of the dependency graph this validation makes no sense. This validation should run even if
            // the requested entry is already built.
            //

            if (FOptions.StrictDI && FPath.Last is not null)
                ServiceErrors.EnsureNotBreaksTheRuleOfStrictDI(FPath.Last, requested);

            if (requested.State.HasFlag(ServiceEntryStates.Built))
                return;
#if DEBUG
            ServiceRequestReplacerDebug FReplacer = new(FLookup, FPath, FOptions.SupportsServiceProvider);
#endif
            //
            // Throws if the request is circular
            //

            FPath.Push(requested);
            try
            {
                requested.VisitFactory(FReplacer.VisitLambda, FactoryVisitorOptions.BuildDelegate);
            }
            finally
            {
                FPath.Pop();
            }

            //
            // No circular reference, no Strict DI violation... entry is validated
            //

            requested.SetValidated();
#if DEBUG
            Debug.WriteLine($"[{requested.ToString(shortForm: true)}] built: visited {FReplacer.VisitedRequests}, altered {FReplacer.AlteredRequests} request(s)");
#endif
        }
    }
}
