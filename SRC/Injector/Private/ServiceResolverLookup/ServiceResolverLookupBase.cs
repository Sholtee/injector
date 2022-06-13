/********************************************************************************
* ServiceResolverLookupBase.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    internal abstract class ServiceResolverLookupBase : IServiceResolverLookup
    {
        private readonly IServiceEntryBuilder FServiceEntryBuilder;

        private readonly object FLock = new();

        private readonly bool FInitialized;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IServiceResolver CreateResolver(AbstractServiceEntry entry) => entry.Features.HasFlag(ServiceEntryFlags.CreateSingleInstance)
            ? entry.Features.HasFlag(ServiceEntryFlags.Shared)
                ? new GlobalScopedServiceResolver(entry, Slots++)
                : new ScopedServiceResolver(entry, Slots++)
            : entry.Features.HasFlag(ServiceEntryFlags.Shared)
                ? new GlobalServiceResolver(entry)
                : new ServiceResolver(entry);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<(Type Interface, string? Name)> RegisterEntries(IEnumerable<AbstractServiceEntry> entries)
        {
            List<(Type, string?)> regularEntries = new();

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
                    regularEntries.Add((entry.Interface, entry.Name));
            }

            return regularEntries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IServiceResolver? GetEarly(Type iface, string? name)
        {
            Assert(!FInitialized, "This method is for initialization purposes only");

            if (TryGetResolver(iface, name, out IServiceResolver resolver))
            {
                //
                // During initialization phase the entry might not have been built.
                //

                if (!resolver.RelatedEntry.State.HasFlag(ServiceEntryStateFlags.Built))
                {
                    FServiceEntryBuilder.Build(resolver.RelatedEntry);
                }

                return resolver;
            }

            if (!iface.IsGenericType || !TryGetGenericEntry(iface.GetGenericTypeDefinition(), name, out AbstractServiceEntry genericEntry))
                return null;

            genericEntry = genericEntry.Specialize(iface.GenericTypeArguments);
            FServiceEntryBuilder.Build(genericEntry);

            AddResolver(resolver = CreateResolver(genericEntry));
            return resolver;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IServiceResolver? GetCore(Type iface, string? name)
        {
            Assert(FInitialized, "This method cannot be called in initialization phase");

            if (TryGetResolver(iface, name, out IServiceResolver resolver))
                return resolver;

            if (!iface.IsGenericType || !TryGetGenericEntry(iface.GetGenericTypeDefinition(), name, out AbstractServiceEntry genericEntry))
                return null;

            lock (FLock)
            {
                //
                // Another thread might have registered the resolver while we reached here.
                //

                if (TryGetResolver(iface, name, out resolver))
                    return resolver;

                genericEntry = genericEntry.Specialize(iface.GenericTypeArguments);
                
                //
                // Build the entry before it gets exposed.
                //

                FServiceEntryBuilder.Build(genericEntry);

                AddResolver(resolver = CreateResolver(genericEntry));
                return resolver;
            }
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

            List<(Type Interface, string? Name)> regularEntries = RegisterEntries(entries);

            //
            // Now it's safe to build (all dependencies are available)
            //

            FServiceEntryBuilder = scopeOptions.ServiceResolutionMode switch
            {
                ServiceEntryBuilder.Id => new ServiceEntryBuilder(this),
                ServiceEntryBuilderAot.Id => new ServiceEntryBuilderAot(this, scopeOptions),
                _ => throw new NotSupportedException()
            };

            foreach ((Type Interface, string? Name) in regularEntries)
            {
                _ = Get(Interface, Name);
            }

            FInitialized = true;
        }

        public int Slots { get; private set; }

        public IServiceResolver? Get(Type iface, string? name)
        {
            IServiceResolver? resolver = FInitialized
                ? GetCore(iface, name)
                : GetEarly(iface, name);

            Assert(resolver?.RelatedEntry.State.HasFlag(ServiceEntryStateFlags.Built) is not false, "Entry must be built when it gets exposed");
            return resolver;
        }
    }
}
