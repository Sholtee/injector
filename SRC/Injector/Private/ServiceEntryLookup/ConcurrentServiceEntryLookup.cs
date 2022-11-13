/********************************************************************************
* ConcurrentServiceEntryLookup.cs                                               *
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

    /// <summary>
    /// Resolver lookup being shared among scopes.
    /// </summary>
    internal class ConcurrentServiceEntryLookup<TEntryLookup> : IServiceEntryLookup where TEntryLookup : class, ILookup<CompositeKey, AbstractServiceEntry>
    {
        //
        // Don't use late binding (ILookup<>) to let the compiler inline
        //

        private volatile TEntryLookup FEntryLookup;

        private readonly TEntryLookup FGenericEntryLookup;

        private readonly IGraphBuilder FGraphBuilder;

        private readonly object FLock = new();

        private readonly bool FInitialized;

        private readonly Func<IServiceEntryLookup, IGraphBuilder> FGraphBuilderFactory;

        private ConcurrentServiceEntryLookup
        (
            TEntryLookup entryLookup,
            TEntryLookup genericEntryLookup,
            Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory
        )
        {
            FEntryLookup = entryLookup;
            FGenericEntryLookup = genericEntryLookup;
            FGraphBuilderFactory = graphBuilderFactory;
            FGraphBuilder = graphBuilderFactory(this);
            FInitialized = true;
        }

        protected ConcurrentServiceEntryLookup
        (
            IEnumerable<AbstractServiceEntry> entries,
            TEntryLookup entryLookup,
            TEntryLookup genericEntryLookup,
            Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory
        )
        {
            FEntryLookup = entryLookup;
            FGenericEntryLookup = genericEntryLookup;
            FGraphBuilderFactory = graphBuilderFactory;
            FGraphBuilder = graphBuilderFactory(this);

            foreach (AbstractServiceEntry entry in entries)
            {
                CompositeKey key = new(entry.Interface, entry.Name);

                if (!(entry.Interface.IsGenericTypeDefinition ? FEntryLookup : FEntryLookup).TryAdd(key, entry))
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

        public ConcurrentServiceEntryLookup<TNewEntryLookup> ChangeBackend<TNewEntryLookup>
        (
            Func<TEntryLookup, TNewEntryLookup> changeLookup,
            Func<IServiceEntryLookup, IGraphBuilder>? graphBuilderFactory = null
        )
            where TNewEntryLookup : class, ILookup<CompositeKey, AbstractServiceEntry>
        =>
            new
            (
                changeLookup(FEntryLookup),
                changeLookup(FGenericEntryLookup),
                graphBuilderFactory ?? FGraphBuilderFactory
            );

        public int Slots => FGraphBuilder.Slots;

        public AbstractServiceEntry? Get(Type iface, string? name)
        {
            CompositeKey key = new(iface, name);

            if (FEntryLookup.TryGet(key, out AbstractServiceEntry? entry))
            {
                //
                // In initialization phase, build the full dependency graph even if the related entry already
                // built.
                // Note that initialization is always single-threaded so there is no need to lock.
                //

                if (!FInitialized)
                {
                    FGraphBuilder.Build(entry);
                }
            }

            else if (iface.IsConstructedGenericType)
            {
                lock (FLock)
                {
                    //
                    // Another thread might have done this work.
                    //

                    if (!FEntryLookup.TryGet(key, out entry))
                    {
                        CompositeKey genericKey = new(iface.GetGenericTypeDefinition(), name);

                        if (FGenericEntryLookup.TryGet(genericKey, out AbstractServiceEntry genericEntry))
                        {
                            genericEntry = genericEntry.Specialize(iface.GenericTypeArguments);

                            //
                            // Build the entry before it gets exposed (before changing the FResolvers instance).
                            //

                            FGraphBuilder.Build(genericEntry);

                            FEntryLookup = FEntryLookup.Add(key, entry);
                        }
                    }
                }
            }

            Debug.Assert(entry?.State.HasFlag(ServiceEntryStates.Built) is not false, "Entry must be built when it gets exposed");
            return entry;
        }
    }

    //
    // Workaroung to enforce "new" constraint
    //

    internal sealed class ConstructableConcurrentServiceResolverLookup<TEntryLookup> : ConcurrentServiceEntryLookup<TEntryLookup>
        where TEntryLookup : class, ILookup<CompositeKey, AbstractServiceEntry>, new()
    {
        public ConstructableConcurrentServiceResolverLookup
        (
            IEnumerable<AbstractServiceEntry> entries,
            Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory
        ) : base
        (
            entries,
            new TEntryLookup(),
            new TEntryLookup(),
            graphBuilderFactory
        ){ }
    }
}
