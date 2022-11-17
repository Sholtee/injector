/********************************************************************************
* ServiceEntryLookup.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    /// <summary>
    /// Resolver lookup intended for initialization purposes
    /// </summary>
    internal sealed class ServiceEntryLookup<TBackend>: IServiceEntryLookup where TBackend : class, ILookup<CompositeKey, AbstractServiceEntry, TBackend>
    {
        private volatile TBackend FEntryLookup;
        private readonly TBackend FGenericEntryLookup;
        private readonly IGraphBuilder FGraphBuilder;
        private readonly bool FInitialized;
        private readonly object FLock = new();
        private int FSlots;

        public ServiceEntryLookup(IEnumerable<AbstractServiceEntry> entries, Func<TBackend> backendFactory, Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory)
        {
            FEntryLookup = backendFactory();
            FGenericEntryLookup = backendFactory();

            foreach (AbstractServiceEntry entry in entries)
            {
                CompositeKey key = new(entry.Interface, entry.Name);

                if (!(entry.Interface.IsGenericTypeDefinition ? FGenericEntryLookup : FEntryLookup).TryAdd(key, entry))
                {
                    InvalidOperationException ex = new(Resources.SERVICE_ALREADY_REGISTERED);
                    ex.Data[nameof(entry)] = entry;
                    throw ex;
                }
            }

            //
            // Now its safe to build (graph builder is able the resolve all the dependencies)
            //

            FGraphBuilder = graphBuilderFactory(this);

            foreach (AbstractServiceEntry entry in entries)
            {
                //
                // In initialization phase, build the full dependency graph even if the related entry already
                // built.
                //

                if (!entry.Interface.IsGenericTypeDefinition)
                {
                    FGraphBuilder.Build(entry);
                }
            }

            FInitialized = true;
        }

        public int Slots => FSlots;

        public int AddSlot() => Interlocked.Increment(ref FSlots) - 1;

        public AbstractServiceEntry? Get(Type iface, string? name)
        {
            CompositeKey
                key = new(iface, name),
                genericKey;

            if (FEntryLookup.TryGet(key, out AbstractServiceEntry entry))
            {
                if (!FInitialized)
                    //
                    // In initialization phase, build the full dependency graph even if the related entry already
                    // built. It is required for strict DI validations.
                    //

                    FGraphBuilder.Build(entry);
            }
            else if (iface.IsConstructedGenericType)
            {
                if (!FInitialized)
                {
                    genericKey = new(iface.GetGenericTypeDefinition(), name);

                    if (FGenericEntryLookup.TryGet(genericKey, out AbstractServiceEntry genericEntry))
                    {
                        entry = genericEntry.Specialize(iface.GenericTypeArguments);
                        FGraphBuilder.Build(entry);

                        bool added = FEntryLookup.TryAdd(key, entry);
                        Debug.Assert(added, "Specialized entry should not exist by now");
                    }
                }
                else
                {
                    lock (FLock)
                    {
                        //
                        // Another thread might have done this work.
                        //

                        if (!FEntryLookup.TryGet(key, out entry))
                        {
                            genericKey = new(iface.GetGenericTypeDefinition(), name);

                            if (FGenericEntryLookup.TryGet(genericKey, out AbstractServiceEntry genericEntry))
                            {
                                entry = genericEntry.Specialize(iface.GenericTypeArguments);
                                FGraphBuilder.Build(entry);

                                FEntryLookup = FEntryLookup.With(key, entry);
                            }
                        }
                    }
                }
            }

            return entry;
        }

        public TBackend EntryLookup => FEntryLookup;

        public TBackend GenericEntryLookup => FGenericEntryLookup;
    }
}
