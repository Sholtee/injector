/********************************************************************************
* ExperimentalScope.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
        IResolveService<ExperimentalScope>,
        IResolveServiceHavingSingleValue<ExperimentalScope>,
        IResolveGenericService<ExperimentalScope>,
        IResolveGenericServiceHavingSingleValue<ExperimentalScope>
    {
        //
        // Dictionary performs much better against int keys
        //

        private readonly IReadOnlyDictionary<int, Func<ExperimentalScope, Type, object>> FResolvers;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        #pragma warning disable CA1307 // Specify StringComparison for clarity
        internal static int HashCombine(Type iface, string? name) => unchecked(iface.GetHashCode() ^ (name?.GetHashCode() ?? 0));
        #pragma warning restore CA1307

        private ServicePath? FPath;

        #region IInstanceFactory
        //
        // It locks all the write operations related to this scope. Reading already produced services
        // can be done parallelly.
        //

        object IInstanceFactory<ExperimentalScope>.Lock { get; } = new();

        object IInstanceFactory<ExperimentalScope>.CreateInstance(AbstractServiceEntry requested)
        {
            object instance;
            IDisposable? lifetime;

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
                    #pragma warning disable CA2000 // Dispose objects before losing scope
                    instance = requested.CreateInstance(this, out lifetime);
                    #pragma warning restore CA2000
                }
                finally
                {
                    FPath.Pop();
                }

                requested.SetValidated();
            }
            else
                #pragma warning disable CA2000 // Dispose objects before losing scope
                instance = requested.CreateInstance(this, out lifetime);
                #pragma warning restore CA2000

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

        public object Get(Type iface, string? name)
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

            int key = HashCombine(iface.IsGenericType ? iface.GetGenericTypeDefinition() : iface, name);

            return FResolvers.TryGetValue(key, out Func<ExperimentalScope, Type, object> factory)
                ? factory(this, iface)
                : null;
        }
        #endregion

        #region Dispose
        //
        // This list should not be thread safe since it is called inside a lock.
        //

        private readonly CaptureDisposable FDisposableStore = new();

        protected override void Dispose(bool disposeManaged)
        {
            FDisposableStore.Dispose();
            base.Dispose(disposeManaged);
        }

        protected override ValueTask AsyncDispose() => FDisposableStore.DisposeAsync();
        #endregion

        public ExperimentalScope(IEnumerable<AbstractServiceEntry> registeredEntries, ScopeOptions options, object? lifetime)
        {
            int
                regularSlots = 0,
                genericSlotsWithSingleValue = 0,
                genericSlots = 0;

            Dictionary<int, Func<ExperimentalScope, Type, object>> resolvers = new();

            foreach (AbstractServiceEntry entry in registeredEntries)
            {
                //
                // Enforce that there is no closed generic service registered. It's required to keep the GetInstance()
                // method simple.
                //

                if (entry.Interface.IsConstructedGenericType)
                    throw new InvalidOperationException(); // TODO: message

                int key = HashCombine(entry.Interface, entry.Name);

                if (entry.Interface.IsGenericTypeDefinition)
                {
                    if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
                    {
                        int slot = genericSlotsWithSingleValue++; // capture an immutable variable
                        resolvers.Add(key, (scope, iface) => scope.ResolveGenericServiceHavingSingleValue(slot, iface, entry));
                    }
                    else
                    {
                        int slot = genericSlots++;
                        resolvers.Add(key, (scope, iface) => scope.ResolveGenericService(slot, iface, entry));
                    }
                }
                else
                {
                    if (entry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance))
                    {
                        int slot = regularSlots++;
                        resolvers.Add(key, (scope, iface) => scope.ResolveServiceHavingSingleValue(slot, entry));
                    }
                    else
                        resolvers.Add(key, (scope, iface) => scope.ResolveService(entry));
                }
            }

            FResolvers = resolvers;
            FRegularSlots = Array<object>.Create(regularSlots);
            FGenericSlotsWithSingleValue = Array<Node<Type, object>>.Create(genericSlotsWithSingleValue);
            FGenericSlots = Array<Node<Type, AbstractServiceEntry>>.Create(genericSlots);

            Options = options;
            Lifetime = lifetime;
        }

        public ExperimentalScope(ExperimentalScope super, object? lifetime)
        {
            FSuper = super;
            FResolvers = super.FResolvers;
            FRegularSlots = Array<object>.Create(super.FRegularSlots.Length);
            FGenericSlotsWithSingleValue = Array<Node<Type, object>>.Create(super.FGenericSlotsWithSingleValue.Length);
            FGenericSlots = Array<Node<Type, AbstractServiceEntry>>.Create(super.FGenericSlots.Length);

            Options = super.Options;
            Lifetime = lifetime;
        }
    }
}
