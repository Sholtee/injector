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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object CreateInstanceCore(AbstractServiceEntry requested)
        {
            object? instance, disposable;

            //
            // At the root of the dependency graph this validation makes no sense.
            //

            AbstractServiceEntry? requestor = FPath?.Last;
            if (Options.StrictDI && requestor?.State.HasFlag(ServiceEntryStates.Validated) is false)
                ServiceErrors.EnsureNotBreaksTheRuleOfStrictDI(requestor, requested, Options.SupportsServiceProvider);

            if (!requested.State.HasFlag(ServiceEntryStates.Built))
                throw new InvalidOperationException(Resources.NOT_BUILT);

            if (!requested.State.HasFlag(ServiceEntryStates.Validated))
            {
                FPath ??= new ServicePath();

                FPath.Push(requested);
                try
                {
                    instance = requested.CreateInstance!(this, out disposable);
                }
                finally
                {
                    FPath.Pop();
                }

                requested.SetValidated();
            }
            else
                instance = requested.CreateInstance!(this, out disposable);

            if (disposable is not null)
            {
                FDisposableStore ??= new CaptureDisposable();
                FDisposableStore.Capture(disposable);
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
            yield return new ContextualServiceEntry(typeof(IScopeFactory), null, (_, _) => this /*factory is always the root*/);
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

            return FResolverLookup.Get(iface, name)?.Invoke(this);
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
        public virtual IInjector CreateScope(object? tag) => new Injector(this, tag);
        #endregion

        #region IHasTag
        public object? Tag { get; }
        #endregion

        public IServiceResolverLookup ServiceResolverLookup => FResolverLookup;

        public Injector(IEnumerable<AbstractServiceEntry> registeredEntries, ScopeOptions options, object? tag)
        {  
            IReadOnlyCollection<AbstractServiceEntry> svcs = new List<AbstractServiceEntry>
            (
                #pragma warning disable CA2214 // Do not call overridable methods in constructors
                GetAllServices(registeredEntries)
                #pragma warning restore CA2214
            );

            FResolverLookup = ServiceResolverLookupBuilder.Build(svcs, options);  
            FSlots  = Array<object>.Create(FResolverLookup.Slots);
            Options = options;
            Tag     = tag;
            FLock   = new object();
        }

        public Injector(Injector super, object? tag)
        {
            FResolverLookup = super.FResolverLookup;
            FSlots    = Array<object>.Create(FResolverLookup.Slots);
            Options   = super.Options;
            Tag       = tag;
            Super     = super;

            //
            // Assuming that the successor is not shared we don't need lock
            //

            FLock = null;
        }
    }
}
