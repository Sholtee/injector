/********************************************************************************
* ServiceResolverLookupBase.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    internal abstract class ServiceResolverLookupBase : IServiceResolverLookup
    {
        private readonly IServiceEntryBuilder FServiceEntryBuilder;

        private readonly object FLock = new();

        private readonly bool FInitialized;

        private IServiceResolver CreateResolver(AbstractServiceEntry entry)
        {
            //
            // When this method is called from a constructor, FServiceEntryBuilder is intentionally NULL.
            // In every other case, we MUST build the entry before it gets exposed.
            //

            FServiceEntryBuilder?.Build(entry);

            return entry.Features.HasFlag(ServiceEntryFlags.CreateSingleInstance)
                ? entry.Features.HasFlag(ServiceEntryFlags.Shared)
                    ? new GlobalScopedServiceResolver(entry, Slots++)
                    : new ScopedServiceResolver(entry, Slots++)

                : entry.Features.HasFlag(ServiceEntryFlags.Shared)
                    ? new GlobalServiceResolver(entry)
                    : new ServiceResolver(entry);
        }

        private List<AbstractServiceEntry> RegisterEntries(IEnumerable<AbstractServiceEntry> entries)
        {
            List<AbstractServiceEntry> regularEntries = new();

            foreach (AbstractServiceEntry entry in entries)
            {
                bool added = entry.Interface.IsGenericTypeDefinition
                    ? TryAddGenericEntry(entry)
                    : TryAddResolver(CreateResolver(entry));
                if (!added)
                {
                    InvalidOperationException ex = new(Resources.SERVICE_ALREADY_REGISTERED);
                    ex.Data[nameof(entry)] = entry;
                    throw ex;
                }

                if (!entry.Interface.IsGenericTypeDefinition)
                    regularEntries.Add(entry);
            }

            return regularEntries;
        }

        /// <summary>
        /// Tries to add the given <paramref name="resolver"/> to the underlying collection.
        /// </summary>
        /// <remarks>This method is called in the lookup initialization phase meaning it shall not deal with parallel access.</remarks>
        protected abstract bool TryAddResolver(IServiceResolver resolver);

        protected abstract void AddResolver(IServiceResolver resolver);

        /// <summary>
        /// Registers the given open generic <paramref name="entry"/>.
        /// </summary>
        /// <remarks>This method is called in the lookup initialization phase meaning it shall not deal with parallel access.</remarks>
        protected abstract bool TryAddGenericEntry(AbstractServiceEntry entry);

        protected abstract bool TryGetResolver(Type iface, string? name, out IServiceResolver resolver);

        protected abstract bool TryGetGenericEntry(Type iface, string? name, out AbstractServiceEntry genericEntry);

        protected ServiceResolverLookupBase(IEnumerable<AbstractServiceEntry> entries, ScopeOptions scopeOptions)
        {
            //
            // Register all the entries without building them.
            //

            List<AbstractServiceEntry> regularEntries = RegisterEntries(entries);

            //
            // Now it's safe to build (all dependencies are available)
            //

            FServiceEntryBuilder = scopeOptions.ServiceResolutionMode switch
            {
                ServiceEntryBuilder.Id => new ServiceEntryBuilder(this),
                ServiceEntryBuilderAot.Id => new ServiceEntryBuilderAot(this, scopeOptions),
                _ => throw new NotSupportedException()
            };

            foreach (AbstractServiceEntry regularEntry in regularEntries)
            {
                _ = Get(regularEntry.Interface, regularEntry.Name);
            }

            FInitialized = true;
        }

        public int Slots { get; private set; }

        public IServiceResolver? Get(Type iface, string? name)
        {
            if (TryGetResolver(iface, name, out IServiceResolver resolver))
            {
                //
                // During initialization phase the entry might not have been built. Note that initialization
                // is single threaded so no lock required.
                //
                // TODO: FIXME: It's an ugly implementation =(
                //

                if (!FInitialized && !resolver.RelatedEntry.State.HasFlag(ServiceEntryStateFlags.Built))
                {
                    FServiceEntryBuilder.Build(resolver.RelatedEntry);
                }

                Debug.Assert(resolver.RelatedEntry.State.HasFlag(ServiceEntryStateFlags.Built), "Returned entry must be built");
                
                return resolver;
            }

            if (!iface.IsGenericType || !TryGetGenericEntry(iface.GetGenericTypeDefinition(), name, out AbstractServiceEntry genericEntry))
                return null;

            lock (FLock)
            {
                //
                // Another thread might have registered the resolver while we reached here.
                //

                if (TryGetResolver(iface, name, out resolver))
                    return resolver;

                AddResolver
                (
                    resolver = CreateResolver
                    (
                        genericEntry.Specialize(iface.GenericTypeArguments)
                    )
                );

                return resolver;
            }
        }
    }
}
