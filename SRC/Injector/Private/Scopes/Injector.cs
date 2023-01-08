/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
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

        //
        // It locks all the write operations related to this scope. Reading already produced services
        // can be done parallelly.
        //

        private readonly object? FLock;

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

            if (!requested.State.HasFlag(ServiceEntryStates.Built))
                throw new InvalidOperationException(Resources.NOT_BUILT);

            if (!requested.State.HasFlag(ServiceEntryStates.Validated))
            {
                Path.Push(requested);
                try
                {
                    instance = requested.CreateInstance!(this, out disposable);
                }
                finally
                {
                    Path.Pop();
                }

                requested.SetValidated();
            }
            else
                instance = requested.CreateInstance!(this, out disposable);

            if (disposable is not null)
                DisposableStore.Capture(disposable);

            if (instance is null)
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.IS_NULL, nameof(instance)));

            if (!requested.Interface.IsInstanceOfType(instance))
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.INVALID_CAST, requested.Interface));
            
            return instance;
        }

        protected virtual IEnumerable<AbstractServiceEntry> BuiltInServices
        {
            get
            {
                yield return new ScopedServiceEntry(typeof(IInjector), null, static (i, _) => i, false);

                //
                // Factory is always the root.
                //

                yield return new SingletonServiceEntry(typeof(IScopeFactory), null, static (i, _) => i, false);


                yield return new SingletonServiceEntry
                (
                    typeof(IReadOnlyCollection<AbstractServiceEntry>),
                    $"{Consts.INTERNAL_SERVICE_NAME_PREFIX}registered_services",
                    (_, _) => ServiceCollection,
                    false
                );

                yield return new ScopedServiceEntry
                (
                    typeof(IEnumerable<>),
                    null,
                    typeof(ServiceEnumerator<>),
                    false
                );
#if DEBUG
                yield return new ScopedServiceEntry
                (
                    typeof(ICollection),
                    "captured_disposables",
                    static (i, _) => ((Injector) i).DisposableStore.CapturedDisposables,
                    false
                );
#endif
            }
        }

        #region IInstanceFactory
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetOrCreateInstance(AbstractServiceEntry requested, int? slot)
        {
            if (slot < FSlots.Length && FSlots[slot.Value] is not null)
                return FSlots[slot.Value]!;

            if (FLock is not null)
                Monitor.Enter(FLock);
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

                    Array.Resize(ref FSlots, ServiceLookup.Slots);

                return FSlots[slot.Value] ??= CreateInstance(requested);
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

            return ServiceLookup
                .Get(iface, name)
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

        public IServiceEntryLookup ServiceLookup { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        public IReadOnlyCollection<AbstractServiceEntry> ServiceCollection { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        public Injector(IEnumerable<AbstractServiceEntry> registeredEntries, ScopeOptions options, object? tag)
        {
            List<AbstractServiceEntry> allServices = new(registeredEntries);
            allServices.AddRange(BuiltInServices);

            ServiceLookup     = ServiceEntryLookupBuilder.Build(allServices, options);  
            FSlots            = Array<object>.Create(ServiceLookup.Slots);
            FLock             = new object();
            Options           = options;
            Tag               = tag;
            ServiceCollection = allServices;
        }

        public Injector(Injector super, object? tag)
        {
            //
            // Assuming this successor is not shared we don't need lock
            //

            FLock             = null;
            ServiceLookup     = super.ServiceLookup;
            FSlots            = Array<object>.Create(ServiceLookup.Slots);
            Options           = super.Options;
            Tag               = tag;
            ServiceCollection = super.ServiceCollection;
            Super             = super;
        }
    }
}
