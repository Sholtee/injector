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

    internal class Injector :
        Disposable,
        IScopeFactory,
        IInstanceFactory
    {
        //
        // This list should not be thread safe since it is invoked inside the write lock.
        //

        private CaptureDisposable? FDisposableStore;

        private ServicePath? FPath; // Not required when AOT building is enabled 

        private object?[] FSlots;

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

                if (!requested.Interface.IsInstanceOfType(instance))
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.INVALID_CAST, requested.Interface));

                requested.UpdateState(ServiceEntryStates.Instantiated);
            }

            return instance;
        }

        protected virtual void RegisterBuiltInServices(IServiceCollection services)
        {
            ServiceOptions suppressDispose = ServiceOptions.Default with { DisposalMode = ServiceDisposalMode.Suppress };
            services
                .Factory<IInjector>(static i => i, Lifetime.Scoped, suppressDispose)
                .Factory<IInstanceFactory>(static i => (IInstanceFactory) i, Lifetime.Scoped, suppressDispose)
                .Factory<IScopeFactory>(static i => (IScopeFactory) i, Lifetime.Singleton, suppressDispose) // create SF from the root only
                .Factory<IServiceResolver>(_ => ServiceResolver, Lifetime.Singleton, suppressDispose)
                .Service(typeof(IEnumerable<>), typeof(ServiceEnumerator<>), Lifetime.Scoped)
#if DEBUG
                .Factory<IReadOnlyCollection<object>>("captured_disposables", static i => ((Injector)i).DisposableStore.CapturedDisposables, Lifetime.Scoped, suppressDispose)
#endif
                ;
        }

        #region IInstanceFactory
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetOrCreateInstance(AbstractServiceEntry requested, int? slot)
        {
            if (slot < FSlots.Length && FSlots[slot.Value] is not null)
                return FSlots[slot.Value]!;

            //
            // In root we need to lock
            //

            if (Super is null)
                Monitor.Enter(requested);
            try
            {
                if (slot is null)
                    return CreateInstance(requested);

                if (slot >= FSlots.Length)
                    //
                    // We reach here when we made a service request that triggered a ServiceLookup update.
                    // Scopes created after the update won't be affected as they allocate their slot array
                    // with the proper size.
                    //

                    Array.Resize(ref FSlots, ServiceResolver.Slots);

                return FSlots[slot.Value] ??= CreateInstance(requested);
            }
            finally
            {
                if (Super is null)
                    Monitor.Exit(requested);
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

            return ServiceResolver
                .Resolve(iface, name)
                ?.ResolveInstance
                ?.Invoke(this);
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

        public ServiceResolver ServiceResolver
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public Injector(IServiceCollection services, ScopeOptions options, object? tag)
        {
            //
            // Copy the collection to be safe to modify it
            //

            services = new ServiceCollection(services);
            RegisterBuiltInServices(services);

            ServiceResolver = ServiceResolver.Create(services, options);  
            FSlots          = Array<object>.Create(ServiceResolver.Slots);
            Options         = options;
            Tag             = tag;
        }

        public Injector(Injector super, object? tag)
        {
            ServiceResolver = super.ServiceResolver;
            FSlots          = Array<object>.Create(ServiceResolver.Slots);
            Options         = super.Options;
            Tag             = tag;
            Super           = super;
        }
    }
}
