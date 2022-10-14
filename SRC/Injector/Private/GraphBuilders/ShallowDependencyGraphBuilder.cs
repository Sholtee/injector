/********************************************************************************
* ShallowDependencyGraphBuilder.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ShallowDependencyGraphBuilder: IGraphBuilder
    {
        private readonly IDelegateCompiler FCompiler;

        public ShallowDependencyGraphBuilder(IDelegateCompiler compiler) => FCompiler = compiler;

        public void Build(AbstractServiceEntry requested)
        {
            Debug.Assert(!requested.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (!requested.Features.HasFlag(ServiceEntryFeatures.SupportsBuild) || requested.State.HasFlag(ServiceEntryStates.Built))
                return;

            requested.Build(FCompiler);
        }
    }
}
