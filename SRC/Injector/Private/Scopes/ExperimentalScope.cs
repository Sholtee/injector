/********************************************************************************
* ExperimentalScope.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal class ExperimentalScope: Disposable
    {
        private readonly object FLock = new();

        //
        // Store objects to handle the rare case when service instance implements the
        // IAsyncDisposable interface only.
        // This list should not be thread safe since it is called inside a lock.
        //

        private readonly IList<object> FCapturedDisposables = new List<object>();

        private protected void CaptureDisposable(object instance)
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

        #region Nodes
        private protected class ServiceEntryNode<TDescendant> where TDescendant: ServiceEntryNode<TDescendant>
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
        private protected class ServiceEntryNode : ServiceEntryNode<ServiceEntryNode>
        {
            public ServiceEntryNode(AbstractServiceEntry entry) : base(entry)
            {
            }
        }

        /// <summary>
        /// Node of runtime specialized (Scoped/Singleton) generic
        /// </summary>
        private protected sealed class ServiceEntryNodeHavingValue : ServiceEntryNode<ServiceEntryNodeHavingValue>
        {
            public ServiceEntryNodeHavingValue(AbstractServiceEntry entry, object value) : base(entry)
            {
                Value = value;
            }

            public readonly object? Value;
        }
        #endregion

        #region ResolveGenericService
        private protected object? ResolveGenericService(ref ServiceEntryNodeHavingValue? node, Type iface, AbstractServiceEntry openEntry)
        {
            Debug.Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

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

            bool releaseLock = !Monitor.IsEntered(FLock);
            if (releaseLock)
                Monitor.Enter(FLock);

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
                        // Specialize the entry if previously it has not been yet.
                        // 

                        specializedEntry = ((ISupportsSpecialization) openEntry).Specialize(null, iface.GenericTypeArguments);
                }

                object value = specializedEntry.CreateInstance(null!);
                CaptureDisposable(value);

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
                    Monitor.Exit(FLock);
            }
        }

        private protected object? ResolveGenericService(ref ServiceEntryNode? node, Type iface, AbstractServiceEntry openEntry)
        {
            Debug.Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

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

            bool releaseLock = !Monitor.IsEntered(FLock);
            if (releaseLock)
                Monitor.Enter(FLock);

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
                        specializedEntry = ((ISupportsSpecialization) openEntry).Specialize(null, iface.GenericTypeArguments);

                        Debug.Assert(node is null);
                        node = new ServiceEntryNode(specializedEntry);
                    }
                }

                object value = specializedEntry.CreateInstance(null!);
                CaptureDisposable(value);    

                return value;
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(FLock);
            }
        }
    }
    #endregion
}
