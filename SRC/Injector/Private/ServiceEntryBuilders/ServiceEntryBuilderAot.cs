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
        private ServicePath Path { get; }

        private ScopeOptions ScopeOptions { get; }
#if !DEBUG
        private ServiceRequestReplacer Replacer { get; }
#endif
        public new const ServiceResolutionMode Id = ServiceResolutionMode.AOT;

        public ServiceEntryBuilderAot(IServiceResolverLookup lookup, ScopeOptions scopeOptions) : base(lookup)
        {
            ScopeOptions = scopeOptions;
            Path = new ServicePath();
#if !DEBUG
            Replacer = new ServiceRequestReplacer(lookup, Path, scopeOptions.SupportsServiceProvider);
#endif
        }

        public override void Build(AbstractServiceEntry entry)
        {
            Debug.Assert(!entry.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (entry.State.HasFlag(ServiceEntryStateFlags.Built))
                return;
#if DEBUG
            ServiceRequestReplacerDebug Replacer = new(FLookup, Path, ScopeOptions.SupportsServiceProvider);
#endif
            //
            // Throws if the request is circular
            //

            Path.Push(entry);

            //
            // TODO: Enforce strict DI rules
            //

            try
            {
                entry.Build(lambda => (LambdaExpression) Replacer.Visit(lambda));
            }
            finally
            {
                Path.Pop();
            }

            //
            // No circular reference, no Strict DI violation... entry is validated
            //

           // entry.SetValidated(); // <- uncomment if StrictDI validation is done above
#if DEBUG
            Debug.WriteLine($"[{entry.ToString(shortForm: true)}] built: visited {Replacer.VisitedRequests}, altered {Replacer.AlteredRequests} request(s)");
#endif
        }
    }
}
