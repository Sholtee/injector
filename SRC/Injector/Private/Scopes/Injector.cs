/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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
        IInstanceFactory
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

        private object CreateInstanceCore(AbstractServiceEntry requested)
        {
            object? instance, lifetime;

            //
            // At the root of the dependency graph this validation makes no sense.
            //

            if (Options.StrictDI && FPath?.Count > 0)
            {
                AbstractServiceEntry requestor = FPath[^1];

                //
                // The requested service should not exist longer than its requestor.
                //

                if (!requestor.State.HasFlag(ServiceEntryStateFlags.Validated) && requested.Lifetime?.CompareTo(requestor.Lifetime!) < 0)
                {
                    RequestNotAllowedException ex = new(Resources.STRICT_DI);
                    ex.Data[nameof(requestor)] = requestor;
                    ex.Data[nameof(requested)] = requested;

                    throw ex;
                }
            }

            if (!requested.State.HasFlag(ServiceEntryStateFlags.Validated))
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
            yield return new ContextualServiceEntry(typeof(IInjector), null, (i, _) => i);
            yield return new ContextualServiceEntry(typeof(IScopeFactory), null, (i, _) => this /*factory is always the root*/);
            yield return new ScopedServiceEntry(typeof(IEnumerable<>), null, typeof(ServiceEnumerator<>), new { registeredServices = new List<AbstractServiceEntry>(registeredEntries) });
#if DEBUG
            yield return new ContextualServiceEntry(typeof(System.Collections.ICollection), "captured_disposables", (i, _) => (((Injector) i).FDisposableStore ??= new()).CapturedDisposables);
#endif
            foreach (AbstractServiceEntry entry in registeredEntries)
            {
                yield return entry;
            }
        }

        #region IInstanceFactory
        object IInstanceFactory.CreateInstance(AbstractServiceEntry requested)
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

        object IInstanceFactory.GetOrCreateInstance(AbstractServiceEntry requested, int slot)
        {
            if (slot < FSlots.Length && FSlots[slot] is not null)
                return FSlots[slot]!;

            if (FLock is not null)
                Monitor.Enter(FLock);
            try
            {
                if (slot < FSlots.Length && FSlots[slot] is not null)
                    return FSlots[slot]!;

                if (slot >= FSlots.Length)
                    //
                    // We reach here when we made a service request that triggered a ResolverCollection update.
                    // Scopes created after the update won't be affected as they allocate their slot array with
                    // the proper size.
                    //

                    Array.Resize(ref FSlots, slot + 1);

                return FSlots[slot] = CreateInstanceCore(requested);
            }
            finally
            {
                if (FLock is not null)
                    Monitor.Exit(FLock);
            }
        }

        public IInstanceFactory? Super { get; }
        #endregion

        #region IInjector
        public ScopeOptions Options { get; }

        public object? Lifetime { get; }

        public virtual object Get(Type iface, string? name)
        {
            object? instance = TryGet(iface, name);

            if (instance is null)
            {
                MissingServiceEntry requested = new(iface, name);

                ServiceNotFoundException ex = new(string.Format(Resources.Culture, Resources.SERVICE_NOT_FOUND, requested.ToString(shortForm: true)));

                ex.Data["requested"] = requested;
                ex.Data["requestor"] = FPath?.Count > 0 ? FPath[^1] : null;
#if DEBUG
                ex.Data["scope"] = this;
#endif
                throw ex;
            }

            return instance;
        }

        public object? TryGet(Type iface, string? name)
        {
            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

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

        //
        // According to performance tests, up to ~80 items btree is faster than dictionary. Assuming that
        // there won't be more than 30 constructed generic service 50 seems a good threshold.
        //
        // Note that built btree is 5-10% slower than the regular one (It would be worth an investigation
        // why).
        //

        public const int BTREE_ITEM_THRESHOLD = 50;

        public Injector(IEnumerable<AbstractServiceEntry> registeredEntries, ScopeOptions options, object? lifetime)
        {
            #pragma warning disable CA2214 // Do not call overridable methods in constructors
            IReadOnlyCollection<AbstractServiceEntry> svcs = new List<AbstractServiceEntry>
            (
                GetAllServices(registeredEntries)
            );
            #pragma warning restore CA2214

            FResolverLookup = (options.Engine ?? (svcs.Count <= BTREE_ITEM_THRESHOLD ? ServiceResolverLookup_BTree.Id : ServiceResolverLookup_Dict.Id)) switch
            {
                ServiceResolverLookup_BTree.Id => new ServiceResolverLookup_BTree(svcs),
                ServiceResolverLookup_BuiltBTree.Id => new ServiceResolverLookup_BuiltBTree(svcs),
                ServiceResolverLookup_Dict.Id  => new ServiceResolverLookup_Dict(svcs),
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
