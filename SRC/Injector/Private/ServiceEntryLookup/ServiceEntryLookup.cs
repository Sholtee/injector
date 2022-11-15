/********************************************************************************
* ServiceEntryLookup.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    /// <summary>
    /// Resolver lookup intended for initialization purposes
    /// </summary>
    internal sealed class ServiceEntryLookup: IServiceEntryLookup
    {
        private readonly ILookup<CompositeKey, AbstractServiceEntry> FEntryLookup;
        private readonly ILookup<CompositeKey, AbstractServiceEntry> FGenericEntryLookup;
        private readonly IGraphBuilder FGraphBuilder;
        private readonly bool FInitialized;

        private ServiceEntryLookup
        (
            IEnumerable<AbstractServiceEntry> entries,
            Func<ILookup<CompositeKey, AbstractServiceEntry>> lookupFactory,
            Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory
        )
        {
            FEntryLookup = lookupFactory();
            FGenericEntryLookup = lookupFactory();

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

        public int Slots { get; private set; }

        public int AddSlot() => Slots++;

        public AbstractServiceEntry? Get(Type iface, string? name)
        {
            CompositeKey key = new(iface, name);

            if (!FEntryLookup.TryGet(key, out AbstractServiceEntry? entry) && iface.IsConstructedGenericType)
            {
                CompositeKey genericKey = new(iface.GetGenericTypeDefinition(), name);

                if (FGenericEntryLookup.TryGet(genericKey, out AbstractServiceEntry genericEntry))
                {
                    entry = genericEntry.Specialize(iface.GenericTypeArguments);

                    FEntryLookup.TryAdd(key, entry);
                }
            }

            //
            // In initialization phase, build the full dependency graph even if the related entry already
            // built. It is required for strict DI validations.
            //

            if (entry is not null && (!FInitialized || !entry.State.HasFlag(ServiceEntryStates.Built)))
            {
                FGraphBuilder.Build(entry);
            }

            return entry;
        }

        public static void InitializeBackend
        (
            IEnumerable<AbstractServiceEntry> entries,
            Func<ILookup<CompositeKey, AbstractServiceEntry>> lookupFactory,
            Func<IServiceEntryLookup, IGraphBuilder> graphBuilderFactory,
            out TEntryLookup entryLookup,
            out TEntryLookup genericEntryLookup,
            out int slots
        )
        {
            ServiceEntryLookup builder = new(entries, lookupFactory, graphBuilderFactory);
            entryLookup = builder.FEntryLookup;
            genericEntryLookup = builder.FGenericEntryLookup;
            slots = builder.Slots;
        }
    }
}
