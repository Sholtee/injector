/********************************************************************************
* ServiceResolverBase.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class ServiceResolverBase : IServiceResolver
    {
        protected readonly object FLock = new();

        protected Func<IInstanceFactory, object> CreateResolver(AbstractServiceEntry entry)
        {
            Func<IInstanceFactory, object> result;

            if (entry.Features.HasFlag(ServiceEntryFlags.CreateSingleInstance))
            {
                int slot = Slots++;
                result = entry.Features.HasFlag(ServiceEntryFlags.Shared)
                    ? fact => (fact.Super ?? fact).GetOrCreateInstance(entry, slot)
                    : fact => fact.GetOrCreateInstance(entry, slot);
            }
            else
                result = entry.Features.HasFlag(ServiceEntryFlags.Shared)
                    ? fact => (fact.Super ?? fact).CreateInstance(entry)
                    : fact => fact.CreateInstance(entry);

            if (!entry.State.HasFlag(ServiceEntryStateFlags.Built))
                entry.Build(_ => _);

            return result;
        }

        public int Slots { get; private set; }

        public abstract Func<IInstanceFactory, object>? Get(Type iface, string? name);
    }
}
