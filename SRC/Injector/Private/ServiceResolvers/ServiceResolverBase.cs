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
            if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
            {
                int slot = Slots++;
                return entry.Flags.HasFlag(ServiceEntryFlags.Shared)
                    ? fact => (fact.Super ?? fact).GetOrCreateInstance(entry, slot)
                    : fact => fact.GetOrCreateInstance(entry, slot);
            }
            else
                return entry.Flags.HasFlag(ServiceEntryFlags.Shared)
                    ? fact => (fact.Super ?? fact).CreateInstance(entry)
                    : fact => fact.CreateInstance(entry);
        }

        public int Slots { get; private set; }

        public abstract Func<IInstanceFactory, object>? Get(Type iface, string? name);
    }
}
