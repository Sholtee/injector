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
        // (a thread may read FSlots while the other may try to lenghen it).
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object CreateInstance(AbstractServiceEntry requested)
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

            if (disposable is not null)
                DisposableStore.Capture(disposable);

            if (!requested.State.HasFlag(ServiceEntryStates.Instantiated))
            {
                //
                // Check the returned insance (only once to improve performance).
                //

                if (instance is null)
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.IS_NULL, nameof(instance)));

                if (!requested.Interface.IsInstanceOfType(instance))  // according to perf tests this check is quite slow
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.INVALID_CAST, requested.Interface));

                requested.UpdateState(ServiceEntryStates.Instantiated);
            }

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

            if (FInstantiationLock is not null)
                Monitor.Enter(FInstantiationLock);
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
                if (FInstantiationLock is not null)
                    Monitor.Exit(FInstantiationLock);
            }
        }

        public IServiceActivator? Super => FSuper;
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
            CheckNotDisposed();

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface)
                throw new ArgumentException(Resources.PARAMETER_NOT_AN_INTERFACE, nameof(iface));

            if (iface.IsGenericTypeDefinition)
                throw new ArgumentException(Resources.PARAMETER_IS_GENERIC, nameof(iface));

            AbstractServiceEntry? entry = FServiceResolver.Resolve(iface, name);
            return entry is not null
                ? GetOrCreateInstance(entry)
                : null;
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

        public Injector(IServiceResolver resolver, ScopeOptions options, object? tag)
        {
            FServiceResolver   = resolver;
            FSlots             = Array<object>.Create(resolver.Slots);
            FInstantiationLock = new object();  // lock required in the root scope only
            Options            = options;
            Tag                = tag;
        }

        public Injector(IServiceCollection services, ScopeOptions options, object? tag): this
        (
            ServiceResolver.Create
            (
                RegisterBuiltInServices(services),
                options
            ),
            options,
            tag
        ) {}

        public Injector(Injector super, object? tag): this(super.FServiceResolver, super.Options, tag)
            => FSuper = super;
    }
}
