﻿/********************************************************************************
* ShallowDependencyGraphBuilder.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ShallowDependencyGraphBuilder: IGraphBuilder
    {
        private readonly IDelegateCompiler FCompiler;

        private int FSlots;

        private static readonly IFactoryVisitor[] FVisitors = new IFactoryVisitor[]
        {
            new MergeProxiesVisitor(),
            new ApplyLifetimeManagerVisitor()
        };

        public ShallowDependencyGraphBuilder(IDelegateCompiler compiler) => FCompiler = compiler;

        public void Build(AbstractServiceEntry requested)
        {
            Debug.Assert(!requested.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (!requested.Features.HasFlag(ServiceEntryFeatures.SupportsBuild) || requested.State.HasFlag(ServiceEntryStates.Built))
                return;

            requested.Build(FCompiler, ref FSlots, FVisitors);
        }

        public int Slots => FSlots;

        public IServiceEntryLookup Lookup => throw new NotSupportedException();
    }
}
