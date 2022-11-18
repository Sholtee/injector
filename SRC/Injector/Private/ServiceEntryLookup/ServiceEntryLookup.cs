/********************************************************************************
* ServiceEntryLookup.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    /// <summary>
    /// Resolver lookup shared among scopes.
    /// </summary>
    internal sealed class ServiceEntryLookup<TBackend>: IServiceEntryLookup where TBackend : class, ILookup<CompositeKey, AbstractServiceEntry, TBackend>
    {
        private volatile TBackend FEntryLookup;
        private readonly TBackend FGenericEntryLookup;
        private readonly IGraphBuilder FGraphBuilder;
        private readonly bool FInitialized;
        private readonly object FLock = new();
        private int FSlots;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AbstractServiceEntry? GetUnsafe(Type iface, string? name)
        {
            Debug.Assert(!FInitialized, "This method is intended for initialization purposes only");

            CompositeKey key = new(iface, name);

            if (!FEntryLookup.TryGet(key, out AbstractServiceEntry entry) && iface.IsConstructedGenericType)
            {
                CompositeKey genericKey = new(iface.GetGenericTypeDefinition(), name);

                if (FGenericEntryLookup.TryGet(genericKey, out AbstractServiceEntry genericEntry))
                {
                    bool added = FEntryLookup.TryAdd
                    (
                        key,
                        entry = genericEntry.Specialize(iface.GenericTypeArguments)
                    );
                    Debug.Assert(added, "Specialized entry should not exist yet");
                }
            }

            if (entry is not null)
                //
                // In initialization phase, build the full dependency graph even if the related entry already
                // built. It is required for strict DI validations.
                //

                FGraphBuilder.Build(entry);

            return entry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AbstractServiceEntry GetSafe(Type iface, string? name)
        {
            CompositeKey key = new(iface, name);

            if (!FEntryLookup.TryGet(key, out AbstractServiceEntry entry) && iface.IsConstructedGenericType)
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
                            entry = genericEntry.Specialize(iface.GenericTypeArguments);
                            FGraphBuilder.Build(entry);

                            FEntryLookup = FEntryLookup.With(key, entry);
                        }
                    }
                }
            }

            return entry;
        }

        public ServiceEntryLookup
        (
            IEnumerable<AbstractServiceEntry> entries,
            Func<TBackend> backendFactory,
            Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory,
            Action<TBackend>? afterConstruction = null
        )
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

            if (afterConstruction is not null)
            {
                afterConstruction(FEntryLookup);
                afterConstruction(FGenericEntryLookup);
            }

            FInitialized = true;
        }

        public int Slots => FSlots;

        public int AddSlot() => Interlocked.Increment(ref FSlots) - 1;

        public AbstractServiceEntry? Get(Type iface, string? name) => FInitialized
            ? GetSafe(iface, name)
            : GetUnsafe(iface, name);
    }
}
