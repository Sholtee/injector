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
        protected readonly IServiceResolverLookup FLookup;

        public const ServiceResolutionMode Id = ServiceResolutionMode.JIT;

        public ServiceEntryBuilder(IServiceResolverLookup lookup)
        {
            FLookup = lookup;
        }

        public virtual void Build(AbstractServiceEntry entry)
        {
            Debug.Assert(!entry.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (!entry.State.HasFlag(ServiceEntryStateFlags.Built))
                entry.Build(_ => _);
        }
    }
}
