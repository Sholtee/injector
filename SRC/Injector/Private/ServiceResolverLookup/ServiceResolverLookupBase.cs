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
        private readonly IServiceEntryVisitor FServiceEntryVisitor;

        private readonly BatchedDelegateCompiler FDelegateCompiler = new();

        private readonly object FLock = new();

        private int FSlots;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ServiceResolver CreateResolver(AbstractServiceEntry entry) => new ServiceResolver
        (
            entry,
            entry.CreateResolver(ref FSlots)
        );

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
        private ServiceResolver? GetEarly(Type iface, string? name)
        {
            Assert(!FInitialized, "This method is for initialization purposes only");

            if (!TryGetResolver(iface, name, out ServiceResolver resolver))
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

            FServiceEntryVisitor.Visit(resolver.RelatedEntry);
            return resolver;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ServiceResolver? GetCore(Type iface, string? name)
        {
            Assert(FInitialized, "This method cannot be called in initialization phase");

            if (TryGetResolver(iface, name, out ServiceResolver resolver))
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

                FServiceEntryVisitor.Visit(genericEntry);
                FDelegateCompiler.Compile();

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
        protected abstract bool TryAddResolver(ServiceResolver resolver);

        protected abstract void AddResolver(ServiceResolver resolver);

        /// <summary>
        /// Registers the given open generic <paramref name="entry"/>.
        /// </summary>
        /// <remarks>This method is called in the lookup initialization phase meaning it shall not deal with parallel access.</remarks>
        protected abstract bool TryAddGenericEntry(AbstractServiceEntry entry);

        protected abstract bool TryGetResolver(Type iface, string? name, out ServiceResolver resolver);

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

            FServiceEntryVisitor = scopeOptions.ServiceResolutionMode switch
            {
                ServiceResolutionMode.JIT => new ShallowServiceEntryVisitor(FDelegateCompiler),
                ServiceResolutionMode.AOT => new RecursiveGraphBuilder(this, FDelegateCompiler, scopeOptions),
                _ => throw new NotSupportedException()
            };

            foreach ((Type Interface, string? Name) in regularEntries)
            {
                _ = Get(Interface, Name);
            }

            //
            // Compile the delegates, assembled by the Get() calls
            //

            FDelegateCompiler.Compile();
            FInitialized = true;
        }
        #endregion

        public int Slots => FSlots;

        public ServiceResolver? Get(Type iface, string? name)
        {
            ServiceResolver? resolver = FInitialized
                ? GetCore(iface, name)
                : GetEarly(iface, name);

            Assert(resolver?.RelatedEntry.State.HasFlag(ServiceEntryStates.Built) is not false, "Entry must be built when it gets exposed");
            return resolver;
        }
    }
}
