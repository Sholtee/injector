/********************************************************************************
* ServiceResolverLookupBase.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class ServiceResolverLookupBase : IServiceResolverLookup
    {
        protected readonly object FLock = new();

        protected IServiceResolver CreateResolver(AbstractServiceEntry entry)
        {
            IServiceResolver result;

            if (entry.Features.HasFlag(ServiceEntryFlags.CreateSingleInstance))
            {
                int slot = Slots++;
                result = entry.Features.HasFlag(ServiceEntryFlags.Shared)
                    ? new GlobalScopedServiceResolver(entry, slot)
                    : new ScopedServiceResolver(entry, slot);
            }
            else
                result = entry.Features.HasFlag(ServiceEntryFlags.Shared)
                    ? new GlobalServiceResolver(entry)
                    : new ServiceResolver(entry);

            if (!entry.State.HasFlag(ServiceEntryStateFlags.Built))
                entry.Build(_ => _);

            return result;
        }

        public int Slots { get; private set; }

        public abstract IServiceResolver? Get(Type iface, string? name);
    }
}
