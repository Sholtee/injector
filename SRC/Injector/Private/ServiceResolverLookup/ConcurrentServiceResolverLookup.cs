﻿/********************************************************************************
* ConcurrentServiceResolverLookup.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    /// <summary>
    /// Resolver lookup being shared among scopes.
    /// </summary>
    internal class ConcurrentServiceResolverLookup<TResolverLookup, TEntryLookup>: IServiceEntryLookup
        where TResolverLookup: class, ILookup<ServiceResolver, TResolverLookup>
        where TEntryLookup: class, ILookup<AbstractServiceEntry, TEntryLookup>
    {
        //
        // Don't use late binding (ILookup<>) to let the compiler inline
        //

        private volatile TResolverLookup FResolvers;

        private readonly TEntryLookup FGenericEntries;

        private readonly IGraphBuilder FGraphBuilder;

        private readonly object FLock = new();

        private readonly bool FInitialized;

        private readonly Func<IServiceEntryLookup, IGraphBuilder> FGraphBuilderFactory;

        private int FSlots;

        private ConcurrentServiceResolverLookup
        (
            TResolverLookup resolvers,
            TEntryLookup genericEntries,
            Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory,
            int slots
        )
        {
            FResolvers = resolvers;
            FGenericEntries = genericEntries;
            FGraphBuilderFactory = graphBuilderFactory;
            FGraphBuilder = graphBuilderFactory(this);
            FSlots = slots;
            FInitialized = true;
        }

        protected ConcurrentServiceResolverLookup
        (
            IEnumerable<AbstractServiceEntry> entries,
            TResolverLookup resolvers,
            TEntryLookup genericEntries,
            Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory
        )
        {
            FResolvers = resolvers;
            FGenericEntries = genericEntries;
            FGraphBuilderFactory = graphBuilderFactory;
            FGraphBuilder = graphBuilderFactory(this);

            foreach (AbstractServiceEntry entry in entries)
            {
                CompositeKey key = new(entry.Interface, entry.Name);

                bool added = entry.Interface.IsGenericTypeDefinition
                    ? FGenericEntries.TryAdd(key, entry)
                    : FResolvers.TryAdd(key, entry.CreateResolver(ref FSlots));
                if (!added)
                {
                    InvalidOperationException ex = new(Resources.SERVICE_ALREADY_REGISTERED);
                    ex.Data[nameof(entry)] = entry;
                    throw ex;
                }
            }

            //
            // Now its safe to build (graph builder is able the resolve all the dependencies)
            //

            foreach (AbstractServiceEntry entry in entries)
            {
                //
                // In initialization phase, build the full dependency graph even if the related entry already
                // built.
                //

                if (!entry.Interface.IsGenericTypeDefinition)
                    FGraphBuilder.Build(entry);
            }

            FInitialized = true;
        }

        public ConcurrentServiceResolverLookup<TNewResolverLookup, TNewEntryLookup> ChangeBackend<TNewResolverLookup, TNewEntryLookup>
        (
            Func<TResolverLookup, TNewResolverLookup> changeResolverLookup,
            Func<TEntryLookup, TNewEntryLookup> changeEntryLookup,
            Func<IServiceEntryLookup, IGraphBuilder>? graphBuilderFactory = null
        )
            where TNewResolverLookup : class, ILookup<ServiceResolver, TNewResolverLookup>
            where TNewEntryLookup : class, ILookup<AbstractServiceEntry, TNewEntryLookup>
        =>
            new
            (
                changeResolverLookup(FResolvers),
                changeEntryLookup(FGenericEntries),
                graphBuilderFactory ?? FGraphBuilderFactory,
                FSlots
            );

        public int Slots => FSlots;

        public ServiceResolver? Get(Type iface, string? name)
        {
            CompositeKey key = new(iface, name);

            if (FResolvers.TryGet(key, out ServiceResolver? resolver))
            {
                //
                // In initialization phase, build the full dependency graph even if the related entry already
                // built.
                // Note that initialization is always single-threaded so there is no need to lock.
                //

                if (!FInitialized)
                {
                    FGraphBuilder.Build(resolver.GetUnderlyingEntry());
                }
            }

            else if (iface.IsConstructedGenericType)
            {
                lock (FLock)
                {
                    //
                    // Another thread might have done this work.
                    //

                    if (!FResolvers.TryGet(key, out resolver))
                    {
                        CompositeKey genericKey = new(iface.GetGenericTypeDefinition(), name);

                        if (FGenericEntries.TryGet(genericKey, out AbstractServiceEntry genericEntry))
                        {
                            genericEntry = genericEntry.Specialize(iface.GenericTypeArguments);

                            //
                            // Build the entry before it gets exposed (before changing the FResolvers instance).
                            //

                            FGraphBuilder.Build(genericEntry);

                            FResolvers = FResolvers.Add
                            (
                                key,
                                resolver = genericEntry.CreateResolver(ref FSlots)
                            );
                        }
                    }
                }
            }

            Debug.Assert(resolver?.GetUnderlyingEntry().State.HasFlag(ServiceEntryStates.Built) is not false, "Entry must be built when it gets exposed");
            return resolver;
        }
    }

    //
    // Workaroung to enforce "new" constraint
    //

    internal sealed class ConstructableConcurrentServiceResolverLookup<TResolverLookup, TEntryLookup> : ConcurrentServiceResolverLookup<TResolverLookup, TEntryLookup>
        where TResolverLookup : class, ILookup<ServiceResolver, TResolverLookup>, new()
        where TEntryLookup : class, ILookup<AbstractServiceEntry, TEntryLookup>, new()
    {
        public ConstructableConcurrentServiceResolverLookup
        (
            IEnumerable<AbstractServiceEntry> entries,
            Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory
        ) : base
        (
            entries,
            new TResolverLookup(),
            new TEntryLookup(),
            graphBuilderFactory
        ){ }
    }
}
