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
        #region Private
        private readonly IServiceEntryBuilder FServiceEntryBuilder;

        private readonly object FLock = new();

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

            if (!TryGetResolver(iface, name, out IServiceResolver resolver))
            {
                if (!iface.IsGenericType || !TryGetGenericEntry(iface.GetGenericTypeDefinition(), name, out AbstractServiceEntry genericEntry))
                    return null;

                AddResolver
                (
                    resolver = CreateResolver
                    (
                        genericEntry.Specialize(iface.GenericTypeArguments)
                    )
                );
            }

            //
            // During initialization phase, Build() may be required for regular entries too.
            //

            FServiceEntryBuilder.Build(resolver.RelatedEntry);
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
                // Build the entry before it gets exposed (before the AddResolver call).
                //

                FServiceEntryBuilder.Build(genericEntry);

                AddResolver
                (
                    resolver = CreateResolver(genericEntry)
                );
                
                return resolver;
            }
        }
        #endregion

        #region Protected
        protected readonly bool FInitialized;

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
                ServiceEntryBuilder.Id => new ServiceEntryBuilder(),
                ServiceEntryBuilderAot.Id => new ServiceEntryBuilderAot(this, scopeOptions),
                _ => throw new NotSupportedException()
            };

            foreach ((Type Interface, string? Name) in regularEntries)
            {
                _ = Get(Interface, Name);
            }

            FInitialized = true;
        }
        #endregion

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
