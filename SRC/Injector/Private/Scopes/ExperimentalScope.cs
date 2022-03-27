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

    internal class ExperimentalScope: Disposable
    {
        private static readonly ConcurrentDictionary<SpecializedEntryKey, AbstractServiceEntry> FSpecializedEntries = new();

        private sealed record SpecializedEntryKey(Type Interface, AbstractServiceEntry Entry);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AbstractServiceEntry Specialize(AbstractServiceEntry entry, Type iface) => FSpecializedEntries.GetOrAdd
        (
            new SpecializedEntryKey(iface, entry),
            static key => ((ISupportsSpecialization) key.Entry).Specialize(null!, key.Interface.GenericTypeArguments)
        );

        private readonly object FLock = new();

        private readonly ExperimentalScope? FParent;

        public ExperimentalScope(IEnumerable<AbstractServiceEntry> registeredEntries)
        {
            int
                genericSlotsWithSingleValue = 0,
                genericSlots = 0;

            //
            // TODO: remove
            //

            foreach (AbstractServiceEntry entry in registeredEntries)
            {
                if (entry.Interface.IsGenericTypeDefinition)
                {
                    if (entry.CreatesSingleInstance)
                        genericSlotsWithSingleValue++;
                    else
                        genericSlots++;
                }
            }

            FGenericSlotsWithSingleValue = new ServiceEntryNodeHavingValue[genericSlotsWithSingleValue];
            FGenericSlots = new ServiceEntryNode[genericSlots];
        }

        public ExperimentalScope(ExperimentalScope parent)
        {
            FParent = parent;
            FGenericSlotsWithSingleValue = new ServiceEntryNodeHavingValue[parent.FGenericSlotsWithSingleValue.Length];
            FGenericSlots = new ServiceEntryNode[parent.FGenericSlots.Length];
        }

        #region Dispose
        //
        // Store objects to handle the rare case when service instance implements the
        // IAsyncDisposable interface only.
        // This list should not be thread safe since it is called inside a lock.
        //

        private readonly IList<object> FCapturedDisposables = new List<object>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CaptureDisposable(object instance)
        {
            if (instance is IDisposable || instance is IAsyncDisposable)
                FCapturedDisposables.Add(instance);
        }

        protected override void Dispose(bool disposeManaged)
        {
            base.Dispose(disposeManaged);

            for (int i = 0; i < FCapturedDisposables.Count; i++)
            {
                switch (FCapturedDisposables[i])
                {
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                    case IAsyncDisposable asyncDisposable:
                        asyncDisposable
                            .DisposeAsync()
                            .AsTask()
                            .GetAwaiter()
                            .GetResult();
                        break;
                }
            }
        }

        protected override async ValueTask AsyncDispose()
        {
            for (int i = 0; i < FCapturedDisposables.Count; i++)
            {
                switch (FCapturedDisposables[i])
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync();
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
        }
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

        #region ResolveGenericService
        private readonly ServiceEntryNodeHavingValue?[] FGenericSlotsWithSingleValue;

        internal static object ResolveGenericServiceHavingSingleValue(ExperimentalScope self, int slot, Type iface, AbstractServiceEntry openEntry)
        {
            Assert(openEntry.Interface.IsGenericTypeDefinition, "Entry must reference an open generic service");
            Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

            //
            // In case of shared entries retrieve the value from the parent.
            //

            if (openEntry.IsShared && self.FParent is not null)
                return ResolveGenericServiceHavingSingleValue(self.FParent, slot, iface, openEntry);

            ref ServiceEntryNodeHavingValue? node = ref self.FGenericSlotsWithSingleValue[slot];

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

            bool releaseLock = !Monitor.IsEntered(self.FLock);
            if (releaseLock)
                Monitor.Enter(self.FLock);

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

                object value = specializedEntry.CreateInstance(null!);
                self.CaptureDisposable(value);

                if (node is null)
                    //
                    // Writing a "ref" variable is atomic
                    //

                    node = new ServiceEntryNodeHavingValue(specializedEntry, value);

                return value;
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(self.FLock);
            }
        }

        private readonly ServiceEntryNode?[] FGenericSlots;

        internal static object ResolveGenericService(ExperimentalScope self, int slot, Type iface, AbstractServiceEntry openEntry)
        {
            Assert(openEntry.Interface.IsGenericTypeDefinition, "Entry must reference an open generic service");
            Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

            if (openEntry.IsShared && self.FParent is not null)
                return ResolveGenericService(self.FParent, slot, iface, openEntry);

            ref ServiceEntryNode? node = ref self.FGenericSlots[slot];

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

            bool releaseLock = !Monitor.IsEntered(self.FLock);
            if (releaseLock)
                Monitor.Enter(self.FLock);

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

                object value = specializedEntry.CreateInstance(null!);
                self.CaptureDisposable(value);    

                return value;
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(self.FLock);
            }
        }
    }
    #endregion
}
