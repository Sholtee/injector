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
        public const ServiceResolutionMode Id = ServiceResolutionMode.JIT;

        public virtual void Build(AbstractServiceEntry entry)
        {
            Debug.Assert(!entry.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (!entry.State.HasFlag(ServiceEntryStates.Built))
                entry.VisitFactory(static _ => _, FactoryVisitorOptions.BuildDelegate);
        }
    }
}
