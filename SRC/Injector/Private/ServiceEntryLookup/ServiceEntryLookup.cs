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
    internal sealed class ServiceEntryLookup<TEntryLookup, TGraphBuilder> : IServiceEntryLookup
        where TEntryLookup : class, ILookup<CompositeKey, AbstractServiceEntry, TEntryLookup>, new()
        where TGraphBuilder : IGraphBuilder
    {
        //
        // Don't use late binding (ILookup<>) to let the compiler inline
        //

        private readonly TEntryLookup FEntryLookup = new();

        private readonly TEntryLookup FGenericEntryLookup = new();

        private readonly TGraphBuilder FGraphBuilder;

        private readonly bool FInitialized;

        private int FSlots;

        public ServiceEntryLookup(IEnumerable<AbstractServiceEntry> entries, Func<IServiceEntryLookup, TGraphBuilder> graphBuilderFactory)
        {
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
                {
                    FGraphBuilder.Build(entry);
                }
            }

            FInitialized = true;
        }

        public int Slots => FSlots;

        public int AddSlot() => FSlots++;

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
    }
}
