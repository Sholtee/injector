/********************************************************************************
* ServiceEntryBuilder.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ServiceEntryBuilder: IServiceEntryBuilder
    {
        protected readonly IDelegateCompiler FCompiler;

        public const ServiceResolutionMode Id = ServiceResolutionMode.JIT;

        public ServiceEntryBuilder(IDelegateCompiler compiler) => FCompiler = compiler;

        public virtual void Build(AbstractServiceEntry entry)
        {
            Debug.Assert(!entry.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (entry.Features.HasFlag(ServiceEntryFeatures.SupportsVisit) && !entry.State.HasFlag(ServiceEntryStates.Built))
                entry.VisitFactory(static _ => _, FCompiler);
        }
    }
}
