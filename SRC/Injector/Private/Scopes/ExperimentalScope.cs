/********************************************************************************
* ExperimentalScope.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Properties;

    internal class ExperimentalScope:
        Disposable,
        IInjector,
        IScopeFactory,
        IResolveService<ExperimentalScope>,
        IResolveServiceHavingSingleValue<ExperimentalScope>,
        IResolveGenericService<ExperimentalScope>,
        IResolveGenericServiceHavingSingleValue<ExperimentalScope>
    {
        //
        // This list should not be thread safe since it is called inside a lock.
        //

        private readonly CaptureDisposable FDisposableStore = new();

        private readonly Func<int, Func<ExperimentalScope, Type, object>?> FGetResolver;

        private ServicePath? FPath;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        #pragma warning disable CA1307 // Specify StringComparison for clarity
        internal static int HashCombine(Type iface, string? name) => unchecked(iface.GetHashCode() ^ (name?.GetHashCode() ?? 0));
        #pragma warning restore CA1307

        protected virtual IEnumerable<AbstractServiceEntry> GetAllServices(IEnumerable<AbstractServiceEntry> registeredEntries)
        {
            yield return new ContextualServiceEntry(typeof(IInjector), null, (i, _) => i);
            yield return new ContextualServiceEntry(typeof(IScopeFactory), null, (i, _) => this /*factory is always the root*/);
            yield return new ScopedServiceEntry(typeof(IEnumerable<>), null, typeof(ServiceEnumerator<>), new { registeredServices = new List<AbstractServiceEntry>(registeredEntries) });
#if DEBUG
            yield return new InstanceServiceEntry(typeof(ICollection), "captured_disposables", FDisposableStore.CapturedDisposables);
#endif
            foreach (AbstractServiceEntry entry in registeredEntries)
            {
                yield return entry;
            }
        }

        #region IInstanceFactory
        //
        // It locks all the write operations related to this scope. Reading already produced services
        // can be done parallelly.
        //

        object IInstanceFactory<ExperimentalScope>.Lock { get; } = new();

        object IInstanceFactory<ExperimentalScope>.CreateInstance(AbstractServiceEntry requested)
        {
            object instance;
            object? lifetime;

            //
            // At the root of the dependency graph this validation makes no sense.
            //

            if (Options.StrictDI && FPath?.Count > 0)
            {
                AbstractServiceEntry requestor = FPath[^1];

                //
                // The requested service should not exist longer than its requestor.
                //

                if (!requestor.Flags.HasFlag(ServiceEntryFlags.Validated) && requested.Lifetime!.CompareTo(requestor.Lifetime!) < 0)
                {
                    RequestNotAllowedException ex = new(Resources.STRICT_DI);
                    ex.Data[nameof(requestor)] = requestor;
                    ex.Data[nameof(requested)] = requested;

                    throw ex;
                }
            }

            if (!requested.Flags.HasFlag(ServiceEntryFlags.Validated))
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
                FDisposableStore.Capture(lifetime);

            return instance;
        }

        private readonly ExperimentalScope? FSuper;

        ExperimentalScope? IInstanceFactory<ExperimentalScope>.Super => FSuper;

        private readonly object?[] FRegularSlots;

        ref object? IInstanceFactory<ExperimentalScope, object?>.GetSlot(int slot) => ref FRegularSlots[slot];

        private readonly Node<Type, object>?[] FGenericSlotsWithSingleValue;

        ref Node<Type, object>? IInstanceFactory<ExperimentalScope, Node<Type, object>?>.GetSlot(int slot) => ref FGenericSlotsWithSingleValue[slot];

        private readonly Node<Type, AbstractServiceEntry>?[] FGenericSlots;

        ref Node<Type, AbstractServiceEntry>? IInstanceFactory<ExperimentalScope, Node<Type, AbstractServiceEntry>?>.GetSlot(int slot) => ref FGenericSlots[slot];
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

            //
            // Most likely screnario: We are looking for a non-generic regular service (IMyService) or we request a
            // generic service (IMyGenericService<TTypeArgument>) that needs to be constructed.
            //

            Func<ExperimentalScope, Type, object>? resolver = FGetResolver
            (
                HashCombine(iface.IsConstructedGenericType ? iface.GetGenericTypeDefinition() : iface, name)
            );

            if (resolver is null && iface.IsConstructedGenericType)
                //
                // Less likely case: We request a registered-as-constructed generic service
                //

                resolver = FGetResolver
                (
                    HashCombine(iface, name)
                );

            return resolver?.Invoke(this, iface);
        }
        #endregion

        #region Dispose
        protected override void Dispose(bool disposeManaged)
        {
            FDisposableStore.Dispose();
            base.Dispose(disposeManaged);
        }

        protected override ValueTask AsyncDispose() => FDisposableStore.DisposeAsync();
        #endregion

        #region IScopeFactory
        public virtual IInjector CreateScope(object? lifetime = null) => new ExperimentalScope(this, lifetime);
        #endregion

        public ExperimentalScope(IEnumerable<AbstractServiceEntry> registeredEntries, ScopeOptions options, object? lifetime)
        {
            int
                regularSlots = 0,
                genericSlotsWithSingleValue = 0,
                genericSlots = 0;

            FGetResolver = ISwitchBuilder<Func<ExperimentalScope, Type, object>>.Default.Instance.Build
            (
                GetResolvers()
            );
            FRegularSlots = Array<object>.Create(regularSlots);
            FGenericSlotsWithSingleValue = Array<Node<Type, object>>.Create(genericSlotsWithSingleValue);
            FGenericSlots = Array<Node<Type, AbstractServiceEntry>>.Create(genericSlots);

            Options = options;
            Lifetime = lifetime;

            IEnumerable<KeyValuePair<int, Func<ExperimentalScope, Type, object>>> GetResolvers()
            {
                foreach (AbstractServiceEntry entry in GetAllServices(registeredEntries))
                {
                    int key = HashCombine(entry.Interface, entry.Name);

                    if (entry.Interface.IsGenericTypeDefinition)
                    {
                        if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
                        {
                            int slot = genericSlotsWithSingleValue++; // capture an immutable variable
                            yield return new KeyValuePair<int, Func<ExperimentalScope, Type, object>>(key, (scope, iface) => scope.ResolveGenericServiceHavingSingleValue(slot, iface, entry));
                        }
                        else
                        {
                            int slot = genericSlots++;
                            yield return new KeyValuePair<int, Func<ExperimentalScope, Type, object>>(key, (scope, iface) => scope.ResolveGenericService(slot, iface, entry));
                        }
                    }
                    else
                    {
                        if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
                        {
                            int slot = regularSlots++;
                            yield return new KeyValuePair<int, Func<ExperimentalScope, Type, object>>(key, (scope, iface) => scope.ResolveServiceHavingSingleValue(slot, entry));
                        }
                        else
                            yield return new KeyValuePair<int, Func<ExperimentalScope, Type, object>>(key, (scope, iface) => scope.ResolveService(entry));
                    }
                }
            }
        }

        public ExperimentalScope(ExperimentalScope super, object? lifetime)
        {
            FSuper = super;
            FGetResolver = super.FGetResolver;
            FRegularSlots = Array<object>.Create(super.FRegularSlots.Length);
            FGenericSlotsWithSingleValue = Array<Node<Type, object>>.Create(super.FGenericSlotsWithSingleValue.Length);
            FGenericSlots = Array<Node<Type, AbstractServiceEntry>>.Create(super.FGenericSlots.Length);

            Options = super.Options;
            Lifetime = lifetime;
        }
    }
}
