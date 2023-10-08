/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Properties;

    //                                        !!!ATTENTION!!!
    //
    // This class is a critical component therefore every modification should be done carefully, with
    // performance in mind.
    // - NO System.Linq
    // - NO System.Reflection
    // - After ANY modifications, run the unit & performance tests to verify there is no regression
    //

    internal class Injector: Disposable, IScopeFactory, IServiceActivator
    {
        #region Private
        //
        // IServiceResolver is thread safe
        //

        private readonly IServiceResolver FServiceResolver;

        private readonly Injector? FSuper;

        private readonly object? FInstantiationLock;

        //
        // These fields are always accessed in a write lock.
        //

        private CaptureDisposable? FDisposableStore;  // Not required when there is no disposable service requested

        private ServicePath? FPath; // Not required when AOT building is enabled 

        //
        // "volatile" is required as slots can be accessed parallelly in the root scope
        // (a thread may read FSlots while the other may try to lengthen it).
        //

        private volatile object?[] FSlots;

        private CaptureDisposable DisposableStore
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FDisposableStore ??= new CaptureDisposable();
        }

        private ServicePath Path
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FPath ??= new ServicePath();
        }

        private Injector(IServiceResolver resolver, ScopeOptions options, object? tag, object? instantiationLock)
        {
            FServiceResolver = resolver;
            FSlots = Array<object>.Create(resolver.Slots);
            FInstantiationLock = instantiationLock;
            Options = options;
            Tag = tag;
        }

#if DEBUG
        internal virtual
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private
#endif
        object CreateInstance(AbstractServiceEntry requested)
        {
            object? instance, disposable;

            if (Options.StrictDI)
            {
                AbstractServiceEntry? requestor = FPath?.Last;
                
                //
                // At the root of the dependency graph this validation makes no sense.
                //

                if (requestor?.State.HasFlag(ServiceEntryStates.Validated) is false)
                    ServiceErrors.EnsureNotBreaksTheRuleOfStrictDI(requestor, requested, Options.SupportsServiceProvider);
            }

            Debug.Assert(requested.State.HasFlag(ServiceEntryStates.Built), "The requested service must be built");

            if (!requested.State.HasFlag(ServiceEntryStates.Validated))
            {
                //
                // Validate the graph
                //

                Path.Push(requested);
                try
                {
                    instance = requested.CreateInstance!(this, out disposable);
                }
                finally
                {
                    Path.Pop();
                }

                requested.UpdateState(ServiceEntryStates.Validated);
            }
            else
                instance = requested.CreateInstance!(this, out disposable);

            if (!requested.State.HasFlag(ServiceEntryStates.Instantiated))
            {
                //
                // Check the returned insance (only once to improve performance).
                //

                if (instance is null)
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.IS_NULL, nameof(instance)));

                if (!requested.Interface.IsInstanceOfType(instance))  // according to perf tests this check is quite slow
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.INVALID_CAST, requested.Interface));

                ServiceEntryStates newState = ServiceEntryStates.Instantiated;

                //
                // Set the "Collected" state here, at once with "Instantiated" to not deceive instantiation shortcut
                // in GetOrCreateInstance()
                //

                if (disposable is not null)
                    newState |= ServiceEntryStates.Collected;

                requested.UpdateState(newState);
            }

            if (disposable is not null)
                DisposableStore.Capture(disposable);

            return instance;
        }
        #endregion

        protected static IServiceCollection RegisterBuiltInServices(IServiceCollection services)
        {
            ServiceOptions suppressDispose = ServiceOptions.Default with { DisposalMode = ServiceDisposalMode.Suppress };

            //
            // Copy the collection to be safe to modify it
            //

            return new ServiceCollection(services)
                .Factory(typeof(IInjector), static (i, _) => i, Lifetime.Scoped, suppressDispose)
                .Factory(typeof(IServiceActivator), static (i, _) => i, Lifetime.Scoped, suppressDispose)
                .Factory(typeof(IScopeFactory), static (i, _) => i, Lifetime.Singleton, suppressDispose) // create SF from the root only
                .Factory(typeof(IServiceResolver), static (i, _) => ((Injector) i).FServiceResolver, Lifetime.Singleton, suppressDispose)
                .Service(typeof(IEnumerable<>), typeof(ServiceEnumerator<>), Lifetime.Scoped)
#if DEBUG
                .Factory(typeof(IReadOnlyCollection<object>), "captured_disposables", static (i, _) => ((Injector) i).DisposableStore.CapturedDisposables, Lifetime.Scoped, suppressDispose)
#endif
                ;
        }

        #region IServiceActivator
        public object GetOrCreateInstance(AbstractServiceEntry requested)
        {
            CheckNotDisposed();

            if (requested is null)
                throw new ArgumentNullException(nameof(requested));

            //
            // Shared entries are resolved from the root scope.
            //

            ServiceEntryFeatures features = requested.Features;
            if (features.HasFlag(ServiceEntryFeatures.Shared) && FSuper is not null)
                return FSuper.GetOrCreateInstance(requested);

            //
            // Although the value of FSlots might be chnaged by another thread while we are doing
            // this check, it won't cause any issues as the new value must contain the same items
            // in the same order.
            // "slot" might be greater than "FSlots.Length" if we request a scoped service that
            // needs to be specialized run-time.
            //

            int slot = requested.AssignedSlot;
            if (features.HasFlag(ServiceEntryFeatures.CreateSingleInstance) && slot < FSlots.Length && FSlots[slot] is not null)
                return FSlots[slot]!;

            //
            // If the requested service had already been instantiated and it's not a disposable means 
            // no lock required.
            // Lock required to:
            //   - Set the belonging slot as an atomic operation
            //   - Build the service path
            //   - Extend the disposable store
            //

            ServiceEntryStates state = requested.State;
            if (!features.HasFlag(ServiceEntryFeatures.CreateSingleInstance) && state.HasFlag(ServiceEntryStates.Instantiated) && !state.HasFlag(ServiceEntryStates.Collected))
                return requested.CreateInstance!(this, out _);

            //
            // Entering to lock is a quite expensive operation (regardless the lock was held or not)
            // so try to enter only once
            // (according to perf tests Monitor.IsEntered() much faster than Monitor.TryEnter())
            //

            bool lockTaken = false;
            if (FInstantiationLock is not null && !Monitor.IsEntered(FInstantiationLock))
            {
                Monitor.TryEnter(FInstantiationLock, Options.ResolutionLockTimeout, ref lockTaken);
                if (!lockTaken)  // We must grab the lock here
                    throw new TimeoutException();
            }
            try
            {
                if (!features.HasFlag(ServiceEntryFeatures.CreateSingleInstance))
                    return CreateInstance(requested);

                if (slot >= FSlots.Length)
                    //
                    // We reach here when we made a service request that triggered a ServiceResolver update.
                    // Scopes created after the update won't be affected as they allocate their slot array
                    // with the proper size.
                    //

                    FSlots = Array<object?>.Resize(FSlots, FServiceResolver.Slots);

                //
                // "??" is required as another thread may have done this work while we reached here.
                //

                return FSlots[slot] ??= CreateInstance(requested);
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(FInstantiationLock);
            }
        }

        public IServiceActivator? Super => FSuper;
        #endregion

        #region IInjector
        public ScopeOptions Options { get; }

        public virtual object Get(Type iface, object? name)
        {
            object? instance = TryGet(iface, name);
            if (instance is null)
                //
                // Cannot provide requestor (FPath.Last) here as Injector.Get() may be called from
                // different threads parallelly.
                //

                ServiceErrors.NotFound(iface, name, requestor: null);

            return instance!;
        }

        public object? TryGet(Type iface, object? name)
        {
            CheckNotDisposed();

            //
            // Do NOT examine the interface deeper here as it has performance costs and we don't want
            // to pay it on each requests.
            //

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            AbstractServiceEntry? entry = FServiceResolver.Resolve(iface, name);
            if (entry is null)
                return null;

            if (!entry.State.HasFlag(ServiceEntryStates.Built))
            {
                //
                // Since the entry is supposed to be built here, something must be wrong with it
                //

                if (iface.IsGenericTypeDefinition)
                    throw new ArgumentException(Resources.PARAMETER_IS_GENERIC, nameof(iface));

                Debug.Fail("Entry must be built");
            }

            return GetOrCreateInstance(entry);
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
        public virtual IInjector CreateScope(object? tag)
        {
            CheckNotDisposed();
            return new Injector(this, tag);
        }
        #endregion

        #region IHasTag
        public object? Tag { get; }
        #endregion

        public Injector(IServiceResolver resolver, ScopeOptions options, object? tag): this
        (
            resolver,
            options,
            tag,
            instantiationLock: new object() // lock required in the root scope only
        ) {}

        public Injector(IServiceCollection services, ScopeOptions options, object? tag): this
        (
            new ServiceResolver
            (
                RegisterBuiltInServices(services),
                options
            ),
            options,
            tag
        ) {}

        public Injector(Injector super, object? tag): this(super.FServiceResolver, super.Options, tag, instantiationLock: null)
            => FSuper = super;
    }
}
