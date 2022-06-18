/********************************************************************************
* ServiceEntryBuilderAot.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceEntryBuilderAot : ServiceEntryBuilder
    {
        private readonly ServicePath FPath;

        private readonly ScopeOptions FOptions;

        private readonly IServiceResolverLookup FLookup;
#if !DEBUG
        private readonly ServiceRequestReplacer FReplacer;
#endif
        public new const ServiceResolutionMode Id = ServiceResolutionMode.AOT;

        public ServiceEntryBuilderAot(IServiceResolverLookup lookup, ScopeOptions options)
        {
            FLookup = lookup;
            FOptions = options;
            FPath = new ServicePath();
#if !DEBUG
            FReplacer = new ServiceRequestReplacer(lookup, FPath, scopeOptions.SupportsServiceProvider);
#endif
        }

        public override void Build(AbstractServiceEntry requested)
        {
            Debug.Assert(!requested.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            //
            // At the root of the dependency graph this validation makes no sense. This validation should run even if
            // the requested entry is already built.
            //

            if (FOptions.StrictDI && FPath.Count > 0)
                ServiceErrors.EnsureNotBreaksTheRuleOfStrictDI(FPath[^1], requested);

            if (requested.State.HasFlag(ServiceEntryStateFlags.Built))
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
                requested.Build(lambda => (LambdaExpression) FReplacer.Visit(lambda));
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
