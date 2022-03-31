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

            if (!requested.State.HasFlag(ServiceEntryStates.Validated))
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
            }
            else
                instance = requested.CreateInstance(null!);

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
                    if (entry.CreatesSingleInstance)
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
                    if (entry.CreatesSingleInstance)
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
            FGenericSlotsWithSingleValue = Array<ServiceEntryNodeHavingValue>.Create(genericSlotsWithSingleValue);
            FGenericSlots = Array<ServiceEntryNode>.Create(genericSlots);

            Options = options;
        }

        public ExperimentalScope(ExperimentalScope parent)
        {
            FParent = parent;
            FCases = parent.FCases;
            FRegularSlots = Array<object>.Create(parent.FRegularSlots.Length);
            FGenericSlotsWithSingleValue = Array<ServiceEntryNodeHavingValue>.Create(parent.FGenericSlotsWithSingleValue.Length);
            FGenericSlots = Array<ServiceEntryNode>.Create(parent.FGenericSlots.Length);

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
        private class ServiceEntryNode<TDescendant> where TDescendant: ServiceEntryNode<TDescendant>
        {
            public ServiceEntryNode(AbstractServiceEntry entry)
            {
                Entry = entry;
            }

            public readonly AbstractServiceEntry Entry;

            //
            // Intentionally not a propery (to make referencing possible)
            //

            public TDescendant? Next;
        }

        /// <summary>
        /// Node of runtime specialized (Transient) generic
        /// </summary>
        private class ServiceEntryNode : ServiceEntryNode<ServiceEntryNode>
        {
            public ServiceEntryNode(AbstractServiceEntry entry) : base(entry)
            {
            }
        }

        /// <summary>
        /// Node of runtime specialized (Scoped/Singleton) generic
        /// </summary>
        private sealed class ServiceEntryNodeHavingValue : ServiceEntryNode<ServiceEntryNodeHavingValue>
        {
            public ServiceEntryNodeHavingValue(AbstractServiceEntry entry, object value) : base(entry)
            {
                Value = value;
            }

            public readonly object? Value;
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

            if (entry.IsShared && FParent is not null)
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

            if (entry.IsShared && FParent is not null)
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
        private readonly ServiceEntryNodeHavingValue?[] FGenericSlotsWithSingleValue;

        internal object ResolveGenericServiceHavingSingleValue(int slot, Type iface, AbstractServiceEntry openEntry)
        {
            Assert(openEntry.Interface.IsGenericTypeDefinition, "Entry must reference an open generic service");
            Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

            //
            // In case of shared entries retrieve the value from the parent.
            //

            if (openEntry.IsShared && FParent is not null)
                return FParent.ResolveGenericServiceHavingSingleValue(slot, iface, openEntry);

            ref ServiceEntryNodeHavingValue? node = ref FGenericSlotsWithSingleValue[slot];

            AbstractServiceEntry? specializedEntry = null;

            while (node is not null)
            {
                if (node.Entry.Interface == iface)
                {
                    //------------------------Singleton/Scoped------------------------------------

                    //
                    // If already produced (singleton/scoped), return the instance.
                    //

                    if (node.Value is not null)
                        return node.Value;

                    //-----------------------------------------------------------------------------

                    //
                    // Else creata a new one in a thread safe manner
                    //

                    specializedEntry = node.Entry;
                    break;
                }
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
                if (specializedEntry is null)
                {
                    //
                    // Another thread may set the entry while we reached here
                    //

                    while (node is not null)
                    {
                        if (node.Entry.Interface == iface)
                        {
                            //------------------------Singleton/Scoped------------------------------------

                            if (node.Value is not null)
                                return node.Value;

                            //----------------------------------------------------------------------------

                            specializedEntry = node.Entry;
                            break;
                        }
                        node = ref node.Next;
                    }

                    if (specializedEntry is null)
                        //
                        // Specialize the entry if previously it has not been yet. Always return the same specialized
                        // entry to not screw up the circular reference validation.
                        // 

                        specializedEntry = Specialize(openEntry, iface);
                }

                object value = CreateInstance(specializedEntry);

                Assert(node is null, "Related node must not be assigned");

                //
                // Writing a "ref" variable is atomic
                //

                node = new ServiceEntryNodeHavingValue(specializedEntry, value);

                return value;
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(FWriteLock);
            }
        }

        private readonly ServiceEntryNode?[] FGenericSlots;

        internal object ResolveGenericService(int slot, Type iface, AbstractServiceEntry openEntry)
        {
            Assert(openEntry.Interface.IsGenericTypeDefinition, "Entry must reference an open generic service");
            Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

            if (openEntry.IsShared && FParent is not null)
                return FParent.ResolveGenericService(slot, iface, openEntry);

            ref ServiceEntryNode? node = ref FGenericSlots[slot];

            AbstractServiceEntry? specializedEntry = null;

            while (node is not null)
            {
                if (node.Entry.Interface == iface)
                {
                    specializedEntry = node.Entry;
                    break;
                }
                node = ref node.Next;
            }

            bool releaseLock = !Monitor.IsEntered(FWriteLock);
            if (releaseLock)
                Monitor.Enter(FWriteLock);

            try
            {
                if (specializedEntry is null)
                {
                    while (node is not null)
                    {
                        if (node.Entry.Interface == iface)
                        {
                            specializedEntry = node.Entry;
                            break;
                        }
                        node = ref node.Next;
                    }

                    if (specializedEntry is null)
                    {
                        specializedEntry = Specialize(openEntry, iface);

                        Assert(node is null);
                        node = new ServiceEntryNode(specializedEntry);
                    }
                }

                return CreateInstance(specializedEntry);
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
