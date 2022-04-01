/********************************************************************************
* ExperimentalScope.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Properties;

    internal class ExperimentalScope : Disposable
    {
        //
        // Dictionary performs much better against int keys
        //

        private static readonly ConcurrentDictionary<int, AbstractServiceEntry> FSpecializedEntries = new();

        private readonly IReadOnlyDictionary<int, Func<ExperimentalScope, Type, object>> FCases;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int HashCombine(object? a, object? b) => unchecked((a?.GetHashCode() ?? 0) ^ (b?.GetHashCode() ?? 0));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AbstractServiceEntry Specialize(AbstractServiceEntry entry, Type iface) => FSpecializedEntries.GetOrAdd
        (
            HashCombine(entry, iface),
            _ => ((ISupportsSpecialization) entry).Specialize(null!, iface.GenericTypeArguments)
        );

        private ServicePath? FPath;

        private object CreateInstance(AbstractServiceEntry requested)
        {
            object instance;

            if (!requested.Flags.HasFlag(ServiceEntryFlags.Validated))
            {
                //
                // At the root of the dependency graph this validation makes no sense.
                //

                if (Options.StrictDI && FPath?.Count > 0)
                {
                    AbstractServiceEntry requestor = FPath[^1];

                    //
                    // The requested service should not exist longer than its requestor.
                    //

                    if (requested.Lifetime!.CompareTo(requestor.Lifetime!) < 0)
                    {
                        RequestNotAllowedException ex = new(Resources.STRICT_DI);
                        ex.Data[nameof(requestor)] = requestor;
                        ex.Data[nameof(requested)] = requested;

                        throw ex;
                    }
                }

                if (FPath is null)
                    FPath = new ServicePath();

                FPath.Push(requested);
                try
                {
                    instance = requested.CreateInstance(null!);
                }
                finally
                {
                    FPath.Pop();
                }

                requested.Flags |= ServiceEntryFlags.Validated;
            }
            else
                instance = requested.CreateInstance(null!);

            if (!requested.Flags.HasFlag(ServiceEntryFlags.SuppressDispose))
                FDisposableStore.Capture(instance);

            return instance;
        }

        //
        // It locks all the write operations related to this scope. Reading already produced services
        // may be done parallelly.
        //

        private readonly object FWriteLock = new();

        private readonly ExperimentalScope? FParent;

        public ExperimentalScope(IEnumerable<AbstractServiceEntry> registeredEntries, ScopeOptions options)
        {
            int
                regularSlots = 0,
                genericSlotsWithSingleValue = 0,
                genericSlots = 0;

            Dictionary<int, Func<ExperimentalScope, Type, object>> cases = new();

            foreach (AbstractServiceEntry entry in registeredEntries)
            {
                //
                // Enforce that there is no closed generic service registered. It's required to keep the GetInstance()
                // method simple.
                //

                if (entry.Interface.IsGenericType && !entry.Interface.IsGenericTypeDefinition)
                    throw new InvalidOperationException(); // TODO: message

                int key = HashCombine(entry.Interface, entry.Name);

                if (entry.Interface.IsGenericTypeDefinition)
                {
                    if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
                    {
                        int slot = genericSlotsWithSingleValue++; // capture an immutable variable
                        cases.Add(key, (scope, iface) => scope.ResolveGenericServiceHavingSingleValue(slot, iface, entry));
                    }
                    else
                    {
                        int slot = genericSlots++;
                        cases.Add(key, (scope, iface) => scope.ResolveGenericService(slot, iface, entry));
                    }
                }
                else
                {
                    if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
                    {
                        int slot = regularSlots++;
                        cases.Add(key, (scope, iface) => scope.ResolveServiceHavingSingleValue(slot, entry));
                    }
                    else
                        cases.Add(key, (scope, iface) => scope.ResolveService(entry));
                }
            }

            FCases = cases;
            FRegularSlots = Array<object>.Create(regularSlots);
            FGenericSlotsWithSingleValue = Array<Node<Type, object>>.Create(genericSlotsWithSingleValue);
            FGenericSlots = Array< Node<Type, AbstractServiceEntry>>.Create(genericSlots);

            Options = options;
        }

        public ExperimentalScope(ExperimentalScope parent)
        {
            FParent = parent;
            FCases = parent.FCases;
            FRegularSlots = Array<object>.Create(parent.FRegularSlots.Length);
            FGenericSlotsWithSingleValue = Array<Node<Type, object>>.Create(parent.FGenericSlotsWithSingleValue.Length);
            FGenericSlots = Array<Node<Type, AbstractServiceEntry>>.Create(parent.FGenericSlots.Length);

            Options = parent.Options;
        }

        public object? GetInstance(Type iface, in string? name)
        {
            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            int key = HashCombine(iface.IsGenericType ? iface.GetGenericTypeDefinition() : iface, name);

            return FCases.TryGetValue(key, out Func<ExperimentalScope, Type, object> factory)
                ? factory(this, iface)
                : null;
        }

        public ScopeOptions Options { get; }

        #region Dispose
        //
        // This list should not be thread safe since it is called inside a lock.
        //

        private readonly CaptureDisposable FDisposableStore = new();

        protected override void Dispose(bool disposeManaged)
        {
            FDisposableStore.Dispose();
            base.Dispose(disposeManaged);
        }

        protected override ValueTask AsyncDispose() => FDisposableStore.DisposeAsync();
        #endregion

        #region Nodes
        private class Node<TKey, TValue>
        {
            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public readonly TKey Key;

            public readonly TValue Value;

            //
            // Intentionally not a propery (to make referencing possible)
            //

            public Node<TKey, TValue>? Next;
        }
        #endregion

        #region ResolveService
        private readonly object?[] FRegularSlots;

        internal object ResolveServiceHavingSingleValue(int slot, AbstractServiceEntry entry)
        {
            Assert(entry.Interface.IsGenericTypeDefinition, "Entry must reference a NON generic service");

            //
            // In case of shared entries retrieve the value from the parent.
            //

            if (entry.Flags.HasFlag(ServiceEntryFlags.Shared) && FParent is not null)
                return FParent.ResolveServiceHavingSingleValue(slot, entry);

            //------------------------Singleton/Scoped------------------------------------

            ref object? value = ref FRegularSlots[slot];
            if (value is not null)
                return value;

            //----------------------------------------------------------------------------

            //
            // If the lock already taken, don't enter again (it would have performance penalty)
            //

            bool releaseLock = !Monitor.IsEntered(FWriteLock);
            if (releaseLock)
                Monitor.Enter(FWriteLock);

            try
            {
                //------------------------Singleton/Scoped------------------------------------

                //
                // Another thread may set the value while we reached here
                //

                #pragma warning disable CA1508 // Since we are in a multi-threaded environment this check is required 
                if (value is null)
                #pragma warning restore CA1508

                //-----------------------------------------------------------------------------
                    value = CreateInstance(entry);

                return value;
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(FWriteLock);
            }
        }

        internal object ResolveService(AbstractServiceEntry entry)
        {
            Assert(entry.Interface.IsGenericTypeDefinition, "Entry must reference a NON generic service");

            if (entry.Flags.HasFlag(ServiceEntryFlags.Shared) && FParent is not null)
                return FParent.ResolveService(entry);

            bool releaseLock = !Monitor.IsEntered(FWriteLock);
            if (releaseLock)
                Monitor.Enter(FWriteLock);

            try
            {
                return CreateInstance(entry);
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(FWriteLock);
            }
        }
        #endregion

        #region ResolveGenericService
        private readonly Node<Type, object>?[] FGenericSlotsWithSingleValue;

        internal object ResolveGenericServiceHavingSingleValue(int slot, Type iface, AbstractServiceEntry openEntry)
        {
            Assert(openEntry.Interface.IsGenericTypeDefinition, "Entry must reference an open generic service");
            Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

            //
            // In case of shared entries retrieve the value from the parent.
            //

            if (openEntry.Flags.HasFlag(ServiceEntryFlags.Shared) && FParent is not null)
                return FParent.ResolveGenericServiceHavingSingleValue(slot, iface, openEntry);

            ref Node<Type, object>? node = ref FGenericSlotsWithSingleValue[slot];

            while (node is not null)
            {
                if (node.Key == iface)
                    return node.Value;
                node = ref node.Next;
            }

            //
            // If the lock already taken, don't enter again (it would have performance penalty)
            //

            bool releaseLock = !Monitor.IsEntered(FWriteLock);
            if (releaseLock)
                Monitor.Enter(FWriteLock);

            try
            {
                //
                // Another thread may set the node while we reached here
                //

                #pragma warning disable CA1508
                while (node is not null)
                #pragma warning restore CA1508
                {
                    if (node.Key == iface)
                        return node.Value;

                    node = ref node.Next;
                }

                //
                // Writing a "ref" variable is atomic
                //

                node = new Node<Type, object>
                (
                    iface,
                    CreateInstance
                    (
                        //
                        // Specialize the entry if previously it has not been yet. Always return the same specialized
                        // entry to not screw up the circular reference validation.
                        //

                        Specialize(openEntry, iface)
                    )
                );

                return node.Value;
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(FWriteLock);
            }
        }

        private readonly Node<Type, AbstractServiceEntry>?[] FGenericSlots;

        internal object ResolveGenericService(int slot, Type iface, AbstractServiceEntry openEntry)
        {
            Assert(openEntry.Interface.IsGenericTypeDefinition, "Entry must reference an open generic service");
            Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

            if (openEntry.Flags.HasFlag(ServiceEntryFlags.Shared) && FParent is not null)
                return FParent.ResolveGenericService(slot, iface, openEntry);

            ref Node<Type, AbstractServiceEntry>? node = ref FGenericSlots[slot];

            while (node is not null && node.Key != iface)
            {
                node = ref node.Next;
            }

            bool releaseLock = !Monitor.IsEntered(FWriteLock);
            if (releaseLock)
                Monitor.Enter(FWriteLock);

            try
            {
                //
                // Another thread may set the node while we reached here.
                //

                while (node is not null && node.Key != iface)
                {
                    node = ref node.Next;
                }

                if (node is null)
                    node = new Node<Type, AbstractServiceEntry>(iface, Specialize(openEntry, iface));

                return CreateInstance(node.Value);
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(FWriteLock);
            }
        }
    }
    #endregion
}
