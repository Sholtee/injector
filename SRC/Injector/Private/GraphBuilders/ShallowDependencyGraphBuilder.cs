/********************************************************************************
* ShallowDependencyGraphBuilder.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ShallowDependencyGraphBuilder : IGraphBuilder
    {
        private readonly IDelegateCompiler FCompiler;

        private readonly IServiceEntryLookup FLookup;

        private static readonly IFactoryVisitor[] FVisitors = new IFactoryVisitor[]
        {
            new MergeProxiesVisitor(),
            new ApplyLifetimeManagerVisitor()
        };

        public ShallowDependencyGraphBuilder(IDelegateCompiler compiler, IServiceEntryLookup lookup)
        {
            FCompiler = compiler;
            FLookup = lookup;
        }

        public void Build(AbstractServiceEntry requested)
        {
            Debug.Assert(!requested.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (!requested.Features.HasFlag(ServiceEntryFeatures.SupportsBuild) || requested.State.HasFlag(ServiceEntryStates.Built))
                return;

            requested.Build(FCompiler, FLookup.AddSlot, FVisitors);
        }

        public IServiceEntryLookup Lookup => throw new NotSupportedException();
    }
}
