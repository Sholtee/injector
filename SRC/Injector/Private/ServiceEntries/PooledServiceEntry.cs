/********************************************************************************
* PooledServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Primitives.Threading;

    //
    // A PooledServiceEntry ket modon is lehet peldanyositva: Egy kulonallo poolban vagy a felhasznalo oldalan.
    //

    internal sealed class PooledServiceEntry : ProducibleServiceEntry, IRequiresServiceAccess
    {
        private object? FInstance; // TODO: remove

        private PooledServiceEntry(PooledServiceEntry entry, IServiceRegistry? owner) : base(entry, owner)
        {
            PoolName = entry.PoolName;
            Flags |= ServiceEntryFlags.CreateSingleInstance;
        }

        public PooledServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceRegistry? owner, string poolName) : base(@interface, name, factory, owner)
        {
            PoolName = poolName;
            Flags |= ServiceEntryFlags.CreateSingleInstance;
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IServiceRegistry? owner, string poolName) : base(@interface, name, implementation, owner)
        {
            PoolName = poolName;
            Flags |= ServiceEntryFlags.CreateSingleInstance;
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, IServiceRegistry? owner, string poolName) : base(@interface, name, implementation, explicitArgs, owner)
        {
            PoolName = poolName;
            Flags |= ServiceEntryFlags.CreateSingleInstance;
        }

        public override object CreateInstance(IInjector scope) // TODO: remove
        {
            Ensure.Parameter.IsNotNull(scope, nameof(scope));
            EnsureProducible();

            if (FInstance is not null)
                throw new InvalidOperationException(); // TODO: uzenet

            //
            // Pool-ban az eredeti factory-t hivjuk
            //

            if (scope.Meta(PooledLifetime.POOL_SCOPE) is true)
                FInstance = Factory!(scope, Interface);

            //
            // Fogyasztoban megszolitjuk a bejegyzeshez tartozo PoolService-t
            //

            else
            {
                //
                // Mivel itt elkerjuk a pool-t magat ezert annak a felszabaditasa biztosan a mi felszabaditasunk utan
                // fog megtortenni: biztonsagosan visszahelyezhetjuk mindig az elkert szervizt a pool-ba
                //

                IPool relatedPool = (IPool) scope.Get
                (
                    typeof(IPool<>).MakeGenericType(Interface), // time consuming but called rarely
                    PoolName
                );

                FInstance = new PoolItemCheckin(relatedPool, relatedPool.Get());
            }

            UpdateState(ServiceEntryFlags.Built);

            return FInstance;
        }

        private sealed class PoolItemCheckin : Disposable
        {
            public PoolItemCheckin(IPool pool, object instance)
            {
                Pool = pool;
                Instance = instance;
            }

            protected override void Dispose(bool disposeManaged)
            {
                base.Dispose(disposeManaged);

                Pool.Return(Instance);
            }

            public IPool Pool { get; }

            public object Instance { get; }
        }

        public override object CreateInstance(IInjector scope, out IDisposable? lifetime)
        {
            if (scope.Parent is ILifetimeManager<object>)

                //
                // In pool, we call the original factory
                //

                return base.CreateInstance(scope, out lifetime);
            else
            {
                //
                // In consumer side we get the item from the pool
                //

                IPool relatedPool = (IPool) scope.Get
                (
                    typeof(IPool<>).MakeGenericType(Interface), // time consuming but called rarely
                    PoolName
                );

                object result = relatedPool.Get();
                lifetime = new PoolItemCheckin(relatedPool, result);
                return result;
            }
        }

        public override object GetSingleInstance() => FInstance ?? throw new InvalidOperationException(); // TODO: remove

        public override AbstractServiceEntry CopyTo(IServiceRegistry registry) => new PooledServiceEntry(this, Ensure.Parameter.IsNotNull(registry, nameof(registry)));

        public override AbstractServiceEntry Specialize(IServiceRegistry? owner, params Type[] genericArguments)
        {
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    owner,
                    PoolName
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    owner,
                    PoolName
                ),
                _ when Factory is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    owner,
                    PoolName
                ),
                _ => throw new NotSupportedException()
            };
        }

        public string PoolName { get; }

        public override Lifetime Lifetime { get; } = Lifetime.Pooled;

        public Func<object, object> ServiceAccess { get; } = instance => instance is IWrapped<object> wrapped
            ? wrapped.Value
            : instance;
    }
}