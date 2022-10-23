/********************************************************************************
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
    internal class ConcurrentServiceResolverLookup<TResolverLookup, TEntryLookup>: IServiceResolverLookup
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

        private readonly Func<IServiceResolverLookup, IGraphBuilder> FGraphBuilderFactory;

        private int FSlots;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ServiceResolver CreateResolver(AbstractServiceEntry entry) => new
        (
            entry,
            entry.CreateResolver(ref FSlots)
        );

        private ConcurrentServiceResolverLookup(TResolverLookup resolvers, TEntryLookup genericEntries, Func<IServiceResolverLookup, IGraphBuilder> graphBuilderFactory)
        {
            FResolvers = resolvers;
            FGenericEntries = genericEntries;
            FGraphBuilderFactory = graphBuilderFactory;
            FGraphBuilder = graphBuilderFactory(this);
            FInitialized = true;
        }

        protected ConcurrentServiceResolverLookup
        (
            IEnumerable<AbstractServiceEntry> entries,
            TResolverLookup resolvers,
            TEntryLookup genericEntries,
            Func<IServiceResolverLookup, IGraphBuilder> graphBuilderFactory
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
                    : FResolvers.TryAdd(key, CreateResolver(entry));
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
                // To enforce strict DI validations don't deal with ServiceEntryStates
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
            Func<IServiceResolverLookup, IGraphBuilder>? graphBuilderFactory = null
        )
            where TNewResolverLookup : class, ILookup<ServiceResolver, TNewResolverLookup>
            where TNewEntryLookup : class, ILookup<AbstractServiceEntry, TNewEntryLookup>
        =>
            new
            (
                changeResolverLookup(FResolvers),
                changeEntryLookup(FGenericEntries),
                graphBuilderFactory ?? FGraphBuilderFactory
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
                //

                if (!FInitialized)
                {
                    FGraphBuilder.Build(resolver.RelatedEntry);
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
                                resolver = CreateResolver(genericEntry)
                            );
                        }
                    }
                }
            }

            Debug.Assert(resolver?.RelatedEntry.State.HasFlag(ServiceEntryStates.Built) is not false, "Entry must be built when it gets exposed");
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
            Func<IServiceResolverLookup, IGraphBuilder> graphBuilderFactory
        ) : base
        (
            entries,
            new TResolverLookup(),
            new TEntryLookup(),
            graphBuilderFactory
        ){ }
    }
}
