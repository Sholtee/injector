/********************************************************************************
* ShallowServiceEntryBuilder.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ShallowServiceEntryBuilder : IServiceEntryBuilder
    {
        public ShallowServiceEntryBuilder(IBuildContext buildContext) => BuildContext = buildContext;

        public IReadOnlyList<IFactoryVisitor> Visitors { get; } = new IFactoryVisitor[]
        {
            new MergeProxiesVisitor(),
            new ApplyLifetimeManagerVisitor()
        };

        public IBuildContext BuildContext { get; }

        public void Build(AbstractServiceEntry requested)
        {
            Debug.Assert(!requested.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (!requested.Features.HasFlag(ServiceEntryFeatures.SupportsBuild) || requested.State.HasFlag(ServiceEntryStates.Built))
                return;

            requested.Build(BuildContext, Visitors);
        }
    }
}
