/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;
    using Properties;

    //                                        !!!ATTENTION!!!
    //
    // This class is a critical component therefore every modification should be done carefully, with
    // performance in mind.
    // - NO System.Linq
    // - NO System.Reflection
    // - After ANY modifications, run the unit & performance tests to verify there is no regression
    //

    internal sealed class ServiceResolver : IServiceResolver, IBuildContext
    {
        #region Private
        private readonly IServiceEntryBuilder FEntryBuilder;
        private readonly DelegateCompiler FCompiler;
        private readonly ConcurrentDictionary<object, AbstractServiceEntry?> FEntries = new();
        private readonly ConcurrentDictionary<Type, IReadOnlyCollection<AbstractServiceEntry>> FNamedServices = new();
        private readonly IReadOnlyCollection<object?> FNames; // all the possible service names including NULL
        private readonly bool FInitialized;
        private readonly Func<object, AbstractServiceEntry?> FValueFactory;
        private readonly object FBuildLock = new();
        private readonly TimeSpan FBuildLockTimeout;
        private int FSlots;

        private sealed class CompositeKey : IServiceId
        {
            public CompositeKey(Type iface, object name)
            {
                Interface = iface;
                Name = name;
            }

            public Type Interface { get; }

            public object Name { get; }

            //
            // DON'T use ServiceIdComparer here as it significantly degrades the performance
            //

            public override int GetHashCode() => unchecked(Interface.GetHashCode() ^ Name.GetHashCode());

            public override bool Equals(object obj) => obj is CompositeKey other && other.Interface == Interface && other.Name.Equals(Name);
        }

        //
        // Dictionary<> is definitely faster against Type keys so try to avoid using CompositeKey
        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetKey(Type iface, object? name) => name is not null
            ? new CompositeKey(iface, name)
            : iface;

        private AbstractServiceEntry? Resolve(object key)
        {
            Type iface;
            object? name;

            if (key is CompositeKey compositeKey)
            {
                iface = compositeKey.Interface;
                name  = compositeKey.Name;
            }
            else
            {
                iface = (Type) key;
                name  = null;
            }

            if (!iface.IsInterface)
                throw new ArgumentException(Resources.PARAMETER_NOT_AN_INTERFACE, nameof(key));

            if (!iface.IsConstructedGenericType)
                return null;

            object genericKey = GetKey(iface.GetGenericTypeDefinition(), name);
            if (!FEntries.TryGetValue(genericKey, out AbstractServiceEntry? genericEntry))
                return null;

            Debug.Assert(genericEntry is not null, "Generic entry cannot be null here");

            //
            // Since IServiceEntryBuilder is not meant to be thread-safe, every write operations
            // need to be exclusive.
            //

            if (!Monitor.TryEnter(FBuildLock, FBuildLockTimeout))
                throw new TimeoutException();
            try
            {
                //
                // Another thread may have done this work while we reached here
                //

                if (!FEntries.TryGetValue(key, out AbstractServiceEntry? specialized))
                {
                    specialized = genericEntry!.Specialize(iface.GenericTypeArguments);
                    FEntryBuilder.Build(specialized);

                    //
                    // AOT resolved dependencies are built in batch.
                    //

                    if (FInitialized)
                        FCompiler.Compile();
                }
                return specialized;
            }
            finally
            {
                Monitor.Exit(FBuildLock);
            }
        }
        #endregion

        public ServiceResolver(IEnumerable<AbstractServiceEntry> entries, ScopeOptions scopeOptions)
        {
            FCompiler = new DelegateCompiler();
            FBuildLockTimeout = scopeOptions.ResolutionLockTimeout;

            //
            // Collect all the possible service names to make ResolveMany() more efficient
            //

            HashSet<object?> names = new();

            foreach (AbstractServiceEntry entry in entries)
            {
                object key = GetKey(entry.Interface, entry.Name);

                if (!FEntries.TryAdd(key, entry))
                {
                    InvalidOperationException ex = new(Resources.SERVICE_ALREADY_REGISTERED);
                    try
                    {
                        ex.Data[nameof(entry)] = entry;
                    }
                    catch (ArgumentException)
                    {
                        //
                        // .NET FW throws if value assigned to Exception.Data is not serializable
                        //

                        Debug.Assert(Environment.Version.Major == 4, "Only .NET FW should complain about serialization");
                        ex.Data[nameof(entry)] = entry.ToString(shortForm: false);
                    }
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

            //
            // AOT resolved dependencies are built in batch.
            //

            FCompiler.Compile();
            FInitialized = true;
        }
        
        #region IBuildContext
        public DelegateCompiler Compiler => FCompiler;

        public int AssignSlot() => Interlocked.Increment(ref FSlots) - 1;
        #endregion

        #region IServiceEntryResolver
        public int Slots => FSlots;

        public AbstractServiceEntry? Resolve(Type iface, object? name)
        {
            AbstractServiceEntry? entry = FEntries.GetOrAdd(GetKey(iface, name), FValueFactory);
            if (entry is not null && !FInitialized)
                //
                // In initialization phase, requested services may be unbuilt.
                //

                FEntryBuilder.Build(entry);

            return entry;
        }

        public IEnumerable<AbstractServiceEntry> ResolveMany(Type iface) => FNamedServices.GetOrAdd(iface, iface =>
        {
            List<AbstractServiceEntry> entries = new(FNames.Count);

            foreach (object? name in FNames)
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
