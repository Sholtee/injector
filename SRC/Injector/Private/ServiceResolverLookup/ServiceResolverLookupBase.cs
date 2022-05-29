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

    internal abstract class ServiceResolverLookupBase : IServiceResolverLookup
    {
        private IServiceEntryBuilder? FServiceEntryBuilder;

        protected readonly object FLock = new();

        /// <summary>
        /// Creates a new resolver for the given <paramref name="entry"/>. Since build is not possible till all the entries were registered,
        /// after initial phase created resolvers shall be passed to <see cref="InitResolvers(IEnumerable{IServiceResolver})"/> method.
        /// </summary>
        protected IServiceResolver CreateResolver(AbstractServiceEntry entry)
        {
            FServiceEntryBuilder?.Build(entry);

            return entry.Features.HasFlag(ServiceEntryFlags.CreateSingleInstance)
                ? entry.Features.HasFlag(ServiceEntryFlags.Shared)
                    ? new GlobalScopedServiceResolver(entry, Slots++)
                    : new ScopedServiceResolver(entry, Slots++)

                : entry.Features.HasFlag(ServiceEntryFlags.Shared)
                    ? new GlobalServiceResolver(entry)
                    : new ServiceResolver(entry);
        }

        /// <summary>
        /// When all the user provided entries are registered, this method should be called on newly built resolvers
        /// </summary>
        protected void InitResolvers(IEnumerable<IServiceResolver> resolvers)
        {
            Debug.Assert(FServiceEntryBuilder is null, "Attempt to initialize the lookup more than once.");

            FServiceEntryBuilder = ResolutionMode switch 
            {
                ServiceEntryBuilder.Id => new ServiceEntryBuilder(this),
                ServiceEntryBuilderAot.Id => new ServiceEntryBuilderAot(this),
                _ => throw new NotSupportedException()
            };

            foreach (IServiceResolver resolver in resolvers)
            {
                FServiceEntryBuilder.Build(resolver.RelatedEntry);
            }
        }

        protected ServiceResolverLookupBase(ServiceResolutionMode resolutionMode) => ResolutionMode = resolutionMode;

        public ServiceResolutionMode ResolutionMode { get; }

        public int Slots { get; private set; }

        public abstract IServiceResolver? Get(Type iface, string? name);
    }
}
