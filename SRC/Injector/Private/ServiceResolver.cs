/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.Diagnostics.Debug;

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
        private readonly ConcurrentDictionary<object, AbstractServiceEntry> FEntries = new();
        private readonly ConcurrentDictionary<Type, IReadOnlyCollection<AbstractServiceEntry>> FNamedServices = new();
        private readonly Dictionary<Type, List<object?>> FKeys; // all the possible service keys including NULL
        private readonly bool FInitialized;
        private readonly Func<object, AbstractServiceEntry> FResolve;
        private readonly Func<Type, IReadOnlyCollection<AbstractServiceEntry>> FResolveMany;
        private readonly object FBuildLock = new();
        private readonly TimeSpan FBuildLockTimeout;
        private int FSlots;

        private static readonly Exception FServiceCannotBeResolved = new();

        private sealed class CompositeKey : IServiceId
        {
            public CompositeKey(Type type, object key)
            {
                Type = type;
                Key = key;
            }

            public Type Type { get; }

            public object Key { get; }

            //
            // DON'T use IServiceId.Comparer here as it significantly degrades the performance
            //

            public override int GetHashCode() => unchecked(Type.GetHashCode() ^ Key.GetHashCode());

            public override bool Equals(object obj) =>
                //
                // When comparing keys use Equals() instead of reference check
                // (for proper string comparison)
                //

                obj is CompositeKey other && other.Type == Type && other.Key.Equals(Key);
        }

        //
        // Dictionary<> is definitely faster against Type keys so try to avoid using CompositeKey
        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetKey(Type type, object? key) => key is not null
            ? new CompositeKey(type, key)
            : type;

        private AbstractServiceEntry ResolveCore(object id)
        {
            Type type;
            object? key;

            if (id is CompositeKey compositeKey)
            {
                type = compositeKey.Type;
                key  = compositeKey.Key;
            }
            else
            {
                type = (Type) id;
                key  = null;
            }

            if (!type.IsConstructedGenericType)
                throw FServiceCannotBeResolved;

            object genericKey = GetKey(type.GetGenericTypeDefinition(), key);
            if (!FEntries.TryGetValue(genericKey, out AbstractServiceEntry? genericEntry))
                //
                // Throw here as we don't want to sotre NULLs
                //

                throw FServiceCannotBeResolved;

            Assert(genericEntry is not null, "Generic entry cannot be null here");
            return genericEntry!.Specialize(type.GenericTypeArguments);
        }

        private IReadOnlyCollection<AbstractServiceEntry> ResolveManyCore(Type type)
        {
            //
            // Consider the following:
            //
            // coll
            //   .Service(typeof(IMyGenericSvc<int>), typeof(MyGenericSvc<int>), Lifetime.Singleton, "cica")
            //   .Service(typeof(IMyGenericSvc<>), typeof(MyGenericSvc<>), Lifetime.Singleton, "cica");
            //   .Service(typeof(IMyGenericSvc<>), typeof(MyGenericSvc<>), Lifetime.Singleton, "kutya");
            // ...
            // scope.Get<IEnumerable<int>>();
            //

            HashSet<object?> allKeys = new();
            if (FKeys.TryGetValue(type, out List<object?> keys))
                keys.ForEach(key => allKeys.Add(key));

            if (type.IsConstructedGenericType && FKeys.TryGetValue(type.GetGenericTypeDefinition(), out keys))
                keys.ForEach(key => allKeys.Add(key));

            if (allKeys.Count is 0)
                throw FServiceCannotBeResolved;

            List<AbstractServiceEntry> entries = new(keys.Count);

            foreach (object? key in allKeys)
            {
                AbstractServiceEntry entry = Resolve(type, key)!;
                Assert(entry is not null, "Entry should exist here");
                entries.Add(entry!);
            }

            return entries;
        }
        #endregion

        public ServiceResolver(IEnumerable<AbstractServiceEntry> entries, ScopeOptions scopeOptions)
        {
            FCompiler = new DelegateCompiler();
            FBuildLockTimeout = scopeOptions.ResolutionLockTimeout;

            //
            // Collect all the possible service names to make ResolveMany() more efficient
            //

            Dictionary<Type, List<object?>> keys = new();

            foreach (AbstractServiceEntry entry in entries)
            {
                object key = GetKey(entry.Type, entry.Key);

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

                        Assert(Environment.Version.Major == 4, "Only .NET FW should complain about serialization");
                        ex.Data[nameof(entry)] = entry.ToString(shortForm: false);
                    }
                    throw ex;
                }

                if (!keys.ContainsKey(entry.Type))
                    keys[entry.Type] = new List<object?>();

                keys[entry.Type].Add(entry.Key);
            }

            FKeys = keys;

            //
            // Converting instance methods to delegate is a quite slow operation so do it
            // only once.
            //

            FResolve = ResolveCore;
            FResolveMany = ResolveManyCore;

            //
            // Now its safe to build (graph builder is able the resolve all the dependencies)
            //

            FEntryBuilder = scopeOptions.ServiceResolutionMode switch
            {
                ServiceResolutionMode.JIT => new ShallowServiceEntryBuilder(this),
                ServiceResolutionMode.AOT => new RecursiveServiceEntryBuilder(this, this, scopeOptions),
                _ => throw new NotSupportedException()
            };
            FEntryBuilder.Init(entries);

            FInitialized = true;
        }
        
        #region IBuildContext
        public DelegateCompiler Compiler => FCompiler;

        public int AssignSlot() => Interlocked.Increment(ref FSlots) - 1;
        #endregion

        #region IServiceEntryResolver
        public int Slots => FSlots;

        public AbstractServiceEntry? Resolve(Type type, object? key)
        {
            if (type.IsGenericTypeDefinition)
                throw new ArgumentException(Resources.PARAMETER_IS_GENERIC, nameof(type));

            AbstractServiceEntry entry;
            try
            {
                entry = FEntries.GetOrAdd(GetKey(type, key), FResolve);
            }
            catch (Exception ex) when (ex == FServiceCannotBeResolved)
            {
                return null;
            }

            //
            // Do NOT rely on the "State" as it can be "Built" while the "CreateInstance" is still NULL
            // [Compile() is called after Build()]
            //

            if (entry.CreateInstance is null)
            {
                //
                // Since IServiceEntryBuilder is not meant to be thread-safe, every write operations
                // need to be exclusive.
                //

                if (!Monitor.TryEnter(FBuildLock, FBuildLockTimeout))
                    throw new TimeoutException();
                try
                {
                    //
                    // Another thread may have already done this work.
                    //

                    if (entry.CreateInstance is null)
                    {
                        FEntryBuilder.Build(entry);

                        //
                        // AOT resolved dependencies are built in batch.
                        //

                        if (FInitialized)
                            FCompiler.Compile();
                    }
                }
                finally
                {
                    Monitor.Exit(FBuildLock);
                }
            }

            if (FInitialized)
            {
                Assert(entry.State.HasFlag(ServiceEntryStates.Built), "Entry must be built");
                Assert(entry.CreateInstance is not null, "CreateInstance must be compiled");
            }

            return entry;
        }

        public IReadOnlyCollection<AbstractServiceEntry>? ResolveMany(Type type)
        {
            try
            {
                return FNamedServices.GetOrAdd(type, FResolveMany);
            }
            catch (Exception ex) when (ex == FServiceCannotBeResolved)
            {
                return null;
            }
        }

        public IServiceEntryBuilder ServiceEntryBuilder => FEntryBuilder;
        #endregion
    }
}
