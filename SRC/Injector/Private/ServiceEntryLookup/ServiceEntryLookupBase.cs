/********************************************************************************
* ServiceEntryLookupBase.cs                                                     *
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
    using Properties;

    internal abstract class ServiceEntryLookupBase<TBackend>: IServiceEntryLookup, IBuildContext where TBackend : class, ILookup<CompositeKey, AbstractServiceEntry, TBackend>
    {
        protected volatile TBackend FEntryLookup;
        protected readonly TBackend FGenericEntryLookup;
        protected readonly IGraphBuilder FGraphBuilder;
        protected readonly IDelegateCompiler FCompiler;
        private readonly object FLock = new();
        private readonly bool FInitialized;
        private int FSlots;

        protected ServiceEntryLookupBase
        (
            IEnumerable<AbstractServiceEntry> entries,
            IDelegateCompiler compiler,
            Func<TBackend> backendFactory,
            Func<IServiceEntryLookup, IBuildContext, IGraphBuilder> graphBuilderFactory
        )
        {
            FEntryLookup = backendFactory();
            FGenericEntryLookup = backendFactory();
            FCompiler = compiler;

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

            FGraphBuilder = graphBuilderFactory(this, this);

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

        #region IBuildContext
        public IDelegateCompiler Compiler => FCompiler;

        public int AssignSlot() => Interlocked.Increment(ref FSlots) - 1;
        #endregion

        #region IServiceEntryLookup
        public int Slots => FSlots;

        public AbstractServiceEntry? Get(Type iface, string? name)
        {
            CompositeKey key = new(iface, name);

            if (FEntryLookup.TryGet(key, out AbstractServiceEntry entry))
            {
                if (!FInitialized)
                    //
                    // In initialization phase, requested services may be unbuilt.
                    //

                    FGraphBuilder.Build(entry);
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
                            entry = genericEntry.Specialize(iface.GenericTypeArguments);
                            FGraphBuilder.Build(entry);

                            FEntryLookup = FEntryLookup.With(key, entry);
                        }
                    }
                }
            }

            Debug.Assert(entry?.State.HasFlag(ServiceEntryStates.Built) is not false, "Returned entry must be built");
            return entry;
        }
        #endregion
    }
}
