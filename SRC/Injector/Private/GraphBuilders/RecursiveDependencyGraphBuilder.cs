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

        private readonly IDelegateCompiler FCompiler;

        private int FSlots;

        public RecursiveDependencyGraphBuilder(IServiceEntryLookup lookup, IDelegateCompiler compiler, ScopeOptions options)
        {
            FOptions = options;
            FPath = new ServicePath();
            FVisitors = new IFactoryVisitor[]
            {
                new MergeProxiesVisitor(),
                new ApplyLifetimeManagerVisitor(),
                new ServiceRequestReplacerVisitor(lookup, FPath, options.SupportsServiceProvider)
            };
            FCompiler = compiler;
        }

        public void Build(AbstractServiceEntry requested)
        {
            Debug.Assert(!requested.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            //
            // At the root of the dependency graph this validation makes no sense. This validation should run even if
            // the requested entry is already built.
            //

            if (FOptions.StrictDI && FPath.Last is not null)
                ServiceErrors.EnsureNotBreaksTheRuleOfStrictDI(FPath.Last, requested, FOptions.SupportsServiceProvider);

            if (!requested.Features.HasFlag(ServiceEntryFeatures.SupportsBuild) || requested.State.HasFlag(ServiceEntryStates.Built))
                return;

            //
            // Throws if the request is circular
            //

            FPath.Push(requested);
            try
            {
                requested.Build(FCompiler, ref FSlots, FVisitors);
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

        public int Slots => FSlots;
    }
}
