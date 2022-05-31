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

        private readonly ScopeOptions FScopeOptions;
#if !DEBUG
        private readonly ServiceRequestReplacer FReplacer;
#endif
        public new const ServiceResolutionMode Id = ServiceResolutionMode.AOT;

        public ServiceEntryBuilderAot(IServiceResolverLookup lookup, ScopeOptions scopeOptions) : base(lookup)
        {
            FScopeOptions = scopeOptions;
            FPath = new ServicePath();
#if !DEBUG
            FReplacer = new ServiceRequestReplacer(lookup, scopeOptions.SupportsServiceProvider);
#endif
        }

        public override void Build(AbstractServiceEntry entry)
        {
            Debug.Assert(!entry.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (entry.State.HasFlag(ServiceEntryStateFlags.Built))
                return;
#if DEBUG
            ServiceRequestReplacerDebug FReplacer = new(FLookup, FScopeOptions.SupportsServiceProvider);
#endif
            //
            // Throws if the request is circular
            //

            FPath.Push(entry);

            //
            // TODO: Enforce strict DI rules
            //

            try
            {
                entry.Build(lambda => (LambdaExpression) FReplacer.Visit(lambda));
            }
            finally
            {
                FPath.Pop();
            }

            //
            // TODO: Set the entry validated
            //
#if DEBUG
            Debug.WriteLine($"[{entry.ToString(shortForm: true)}] built: visited {FReplacer.VisitedRequests}, altered {FReplacer.AlteredRequests} request(s)");
#endif
        }
    }
}
