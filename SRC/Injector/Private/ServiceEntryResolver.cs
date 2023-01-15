/********************************************************************************
* ServiceEntryResolver.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal sealed class ServiceEntryResolver : IServiceEntryResolver, IBuildContext
    {
        private readonly IServiceEntryBuilder FEntryBuilder;
        private readonly IDelegateCompiler FCompiler;
        private readonly ConcurrentDictionary<CompositeKey, AbstractServiceEntry?> FEntries = new();
        private readonly ConcurrentDictionary<Type, IReadOnlyCollection<AbstractServiceEntry>> FNamedServices = new();
        private readonly IReadOnlyCollection<string?> FNames; // all the possible service names including NULL
        private readonly bool FInitialized;
        private readonly Func<CompositeKey, AbstractServiceEntry?> FValueFactory;
        private int FSlots;

        private AbstractServiceEntry? Resolve(CompositeKey key)
        {
            if (!key.Interface.IsConstructedGenericType)
                return null;

            CompositeKey genericKey = new(key.Interface.GetGenericTypeDefinition(), key.Name);
            if (!FEntries.TryGetValue(genericKey, out AbstractServiceEntry? genericEntry))
                return null;

            //
            // The factory function may be invoked multiple times:
            // https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd?view=net-7.0
            //
            // And we don't want unnecessary AssignSlot() calls
            //

            lock (genericEntry!)
            {
                if (!FEntries.TryGetValue(key, out AbstractServiceEntry? specialized))
                {
                    specialized = genericEntry.Specialize(key.Interface.GenericTypeArguments);
                    FEntryBuilder.Build(specialized);
                }
                return specialized;
            }
        }

        public ServiceEntryResolver
        (
            IEnumerable<AbstractServiceEntry> entries,
            IDelegateCompiler compiler,
            ScopeOptions scopeOptions
        )
        {
            FCompiler = compiler;

            //
            // Collect all the possible service names to make ResolveMany() more efficient
            //

            HashSet<string?> names = new();

            foreach (AbstractServiceEntry entry in entries)
            {
                CompositeKey key = new(entry.Interface, entry.Name);

                if (!FEntries.TryAdd(key, entry))
                {
                    InvalidOperationException ex = new(Resources.SERVICE_ALREADY_REGISTERED);
                    ex.Data[nameof(entry)] = entry;
                    throw ex;
                }

                names.Add(entry.Name);
            }

            FNames = names;

            //
            // Converting instance methods to delegate is a quite slow operation so do it
            // only once.
            //

            FValueFactory = Resolve;

            //
            // Now its safe to build (graph builder is able the resolve all the dependencies)
            //

            FEntryBuilder = scopeOptions.ServiceResolutionMode switch
            {
                ServiceResolutionMode.JIT => new ShallowServiceEntryBuilder(this),
                ServiceResolutionMode.AOT => new RecursiveServiceEntryBuilder(this, this, scopeOptions),
                _ => throw new NotSupportedException()
            };

            foreach (AbstractServiceEntry entry in entries)
            {
                //
                // In initialization phase, build the full dependency graph even if the related entry already
                // built.
                //

                if (!entry.Interface.IsGenericTypeDefinition)
                {
                    FEntryBuilder.Build(entry);
                }
            }

            FInitialized = true;
        }

        #region IBuildContext
        public IDelegateCompiler Compiler => FCompiler;

        public int AssignSlot() => Interlocked.Increment(ref FSlots) - 1;
        #endregion

        #region IServiceEntryResolver
        public int Slots => FSlots;

        public AbstractServiceEntry? Resolve(Type iface, string? name)
        {
            Debug.Assert(!iface.IsGenericTypeDefinition, "Only specialized types can be resolved");

            AbstractServiceEntry? entry = FEntries.GetOrAdd(new CompositeKey(iface, name), FValueFactory);
            if (entry is not null && !FInitialized)
                //
                // In initialization phase, requested services may be unbuilt.
                //

                FEntryBuilder.Build(entry);

            Debug.Assert(entry?.State.HasFlag(ServiceEntryStates.Built) is not false, "Returned entry must be built");
            return entry;
        }

        public IEnumerable<AbstractServiceEntry> ResolveMany(Type iface) => FNamedServices.GetOrAdd(iface, iface =>
        {
            List<AbstractServiceEntry> entries = new();

            foreach (string? name in FNames)
            {
                AbstractServiceEntry? entry = Resolve(iface, name);
                if (entry is not null)
                    entries.Add(entry);
            }

            return entries;
        });
        #endregion
    }
}
