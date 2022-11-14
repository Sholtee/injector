/********************************************************************************
* ConcurrentServiceEntryLookup.cs                                               *
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

    /// <summary>
    /// Resolver lookup being shared among scopes.
    /// </summary>
    internal class ConcurrentServiceEntryLookup<TEntryLookup, TGraphBuilder> : IServiceEntryLookup
        where TEntryLookup : class, ILookup<CompositeKey, AbstractServiceEntry, TEntryLookup>
        where TGraphBuilder : IGraphBuilder
    {
        //
        // Don't use late binding (ILookup<>) to let the compiler inline
        //

        private volatile TEntryLookup FEntryLookup;

        private readonly TEntryLookup FGenericEntryLookup;

        private readonly TGraphBuilder FGraphBuilder;

        private readonly object FLock = new();

        private int FSlots = -1;

        protected ConcurrentServiceEntryLookup
        (
            IEnumerable<AbstractServiceEntry> entries,
            TEntryLookup entryLookup,
            TEntryLookup genericEntryLookup,
            Func<IServiceEntryLookup, TGraphBuilder> graphBuilderFactory
        )
        {
            FEntryLookup = entryLookup;
            FGenericEntryLookup = genericEntryLookup;
            FGraphBuilder = graphBuilderFactory(this);
        }

        public int AddSlot() => Interlocked.Increment(ref FSlots);

        public int Slots => FSlots + 1;

        public AbstractServiceEntry? Get(Type iface, string? name)
        {
            CompositeKey key = new(iface, name);

            if (!FEntryLookup.TryGet(key, out AbstractServiceEntry? entry) && iface.IsConstructedGenericType)
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

                            //
                            // Build the entry before it gets exposed (before changing the FResolvers instance).
                            //

                            FGraphBuilder.Build(entry);

                            FEntryLookup = FEntryLookup.Add(key, entry);
                        }
                    }
                }
            }

            Debug.Assert(entry?.State.HasFlag(ServiceEntryStates.Built) is not false, "Entry must be built when it gets exposed");
            return entry;
        }
    }
}
