/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Properties;

    internal class Injector:
        Disposable,
        IInjector,
        IScopeFactory,
        IInstanceFactory,
        IServiceEntryLookup
    {
        //
        // This list should not be thread safe since it is invoked inside the write lock.
        //

        private CaptureDisposable? FDisposableStore;

        private readonly IServiceResolverLookup FResolverLookup;

        private ServicePath? FPath;

        private object?[] FSlots;

        //
        // It locks all the write operations related to this scope. Reading already produced services
        // can be done parallelly.
        //

        private readonly object? FLock;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object CreateInstanceCore(AbstractServiceEntry requested)
        {
            object? instance, lifetime;

            //
            // At the root of the dependency graph this validation makes no sense.
            //

            if (Options.StrictDI && FPath?.Last is not null)
                ServiceErrors.EnsureNotBreaksTheRuleOfStrictDI(FPath.Last, requested);

            if (!requested.State.HasFlag(ServiceEntryStates.Validated))
            {
                FPath ??= new ServicePath();

                FPath.Push(requested);
                try
                {
                    instance = requested.CreateInstance(this, out lifetime);
                }
                finally
                {
                    FPath.Pop();
                }

                requested.SetValidated();
            }
            else
                instance = requested.CreateInstance(this, out lifetime);

            if (lifetime is not null)
            {
                FDisposableStore ??= new CaptureDisposable();
                FDisposableStore.Capture(lifetime);
            }

            if (instance is null)
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.IS_NULL, nameof(instance)));
/*
            if (requested.Interface.IsInstanceOfType(instance))
                throw new InvalidOperationException();
*/
            return instance;
        }

        protected virtual IEnumerable<AbstractServiceEntry> GetAllServices(IEnumerable<AbstractServiceEntry> registeredEntries)
        {
            yield return new ContextualServiceEntry(typeof(IInjector), null, static (i, _) => i);
            yield return new ContextualServiceEntry(typeof(IScopeFactory), null, (i, _) => this /*factory is always the root*/);
            yield return new ScopedServiceEntry(typeof(IEnumerable<>), null, typeof(ServiceEnumerator<>), new { registeredServices = new List<AbstractServiceEntry>(registeredEntries) });
#if DEBUG
            yield return new ContextualServiceEntry(typeof(System.Collections.ICollection), "captured_disposables", static (i, _) => (((Injector) i).FDisposableStore ??= new()).CapturedDisposables);
#endif
            foreach (AbstractServiceEntry entry in registeredEntries)
            {
                yield return entry;
            }
        }

        #region IInstanceFactory
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object CreateInstance(AbstractServiceEntry requested)
        {
            //
            // In the same thread locks can be taken recursively.
            //

            if (FLock is not null)
                Monitor.Enter(FLock);
            try
            {
                return CreateInstanceCore(requested);
            }
            finally
            {
                if (FLock is not null)
                    Monitor.Exit(FLock);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetOrCreateInstance(AbstractServiceEntry requested, int slot)
        {
            if (slot < FSlots.Length && FSlots[slot] is not null)
                return FSlots[slot]!;

            if (FLock is not null)
                Monitor.Enter(FLock);
            try
            {
                if (slot >= FSlots.Length)
                    //
                    // We reach here when we made a service request that triggered a ResolverCollection update.
                    // Scopes created after the update won't be affected as they allocate their slot array with
                    // the proper size.
                    //

                    Array.Resize(ref FSlots, slot + 1);

                return FSlots[slot] ??= CreateInstanceCore(requested);
            }
            finally
            {
                if (FLock is not null)
                    Monitor.Exit(FLock);
            }
        }

        public IInstanceFactory? Super
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }
        #endregion

        #region IInjector
        public ScopeOptions Options { get; }

        public object? Lifetime { get; }

        public virtual object Get(Type iface, string? name)
        {
            object? instance = TryGet(iface, name);
            if (instance is null)
                ServiceErrors.NotFound(iface, name, FPath?.Last);

            return instance!;
        }

        public object? TryGet(Type iface, string? name)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface)
                throw new ArgumentException(Resources.PARAMETER_NOT_AN_INTERFACE, nameof(iface));

            if (iface.IsGenericTypeDefinition)
                throw new ArgumentException(Resources.PARAMETER_IS_GENERIC, nameof(iface));

            return FResolverLookup.Get(iface, name)?.Resolve(this);
        }
        #endregion

        #region Dispose
        protected override void Dispose(bool disposeManaged)
        {
            FDisposableStore?.Dispose();
            base.Dispose(disposeManaged);
        }

        protected override ValueTask AsyncDispose() => FDisposableStore?.DisposeAsync() ?? default;
        #endregion

        #region IScopeFactory
        public virtual IInjector CreateScope(object? lifetime) => new Injector(this, lifetime);
        #endregion

        #region IServiceEntryLookup
        AbstractServiceEntry? IServiceEntryLookup.Get(Type iface, string? name) => FResolverLookup.Get(iface, name)?.RelatedEntry;
        #endregion

        //
        // According to performance tests, up to ~50 items built btree is faster than dictionary.
        // Assuming that there won't be more than 20 constructed generic service 30 seems a good
        // threshold.
        //

        public const int BTREE_ITEM_THRESHOLD = 30;

        public Injector(IEnumerable<AbstractServiceEntry> registeredEntries, ScopeOptions options, object? lifetime)
        {
            
            IReadOnlyCollection<AbstractServiceEntry> svcs = new List<AbstractServiceEntry>
            (
                #pragma warning disable CA2214 // Do not call overridable methods in constructors
                GetAllServices(registeredEntries)
                #pragma warning restore CA2214
            );
            
            #pragma warning disable CA1304 // Specify CultureInfo
            FResolverLookup = (options.Engine?.ToLower() ?? (svcs.Count <= BTREE_ITEM_THRESHOLD ? ServiceResolverLookup_BuiltBTree.Id : ServiceResolverLookup_Dict.Id)) switch
            #pragma warning restore CA1304 // Specify CultureInfo
            {
                ServiceResolverLookup_BTree.Id => new ServiceResolverLookup_BTree(svcs, options),
                ServiceResolverLookup_BuiltBTree.Id => new ServiceResolverLookup_BuiltBTree(svcs, options),
                ServiceResolverLookup_Dict.Id  => new ServiceResolverLookup_Dict(svcs, options),
                _ => throw new NotSupportedException()
            };

            FSlots    = Array<object>.Create(FResolverLookup.Slots);
            Options   = options;
            Lifetime  = lifetime;
            FLock     = new object();
        }

        public Injector(Injector super, object? lifetime)
        {
            FResolverLookup = super.FResolverLookup;
            FSlots    = Array<object>.Create(FResolverLookup.Slots);
            Options   = super.Options;
            Lifetime  = lifetime;
            Super     = super;

            //
            // Assuming that the successor is not shared we don't need lock
            //

            FLock = null;
        }
    }
}
