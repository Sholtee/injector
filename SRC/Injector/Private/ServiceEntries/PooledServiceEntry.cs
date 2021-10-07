/********************************************************************************
* PooledServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    //
    // A PooledServiceEntry ket modon is lehet peldanyositva: Egy kulonallo poolban vagy a felhasznalo oldalan.
    //

    internal sealed class PooledServiceEntry : ProducibleServiceEntry, IRequiresServiceAccess
    {
        private object? FInstance;

        private PooledServiceEntry(PooledServiceEntry entry, IServiceRegistry? owner) : base(entry, owner)
        {
            PoolName = entry.PoolName;
        }

        public PooledServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceRegistry? owner, string poolName) : base(@interface, name, factory, owner)
        {
            PoolName = poolName;
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IServiceRegistry? owner, string poolName) : base(@interface, name, implementation, owner)
        {
            PoolName = poolName;
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceRegistry? owner, string poolName) : base(@interface, name, implementation, explicitArgs, owner)
        {
            PoolName = poolName;
        }

        public override object CreateInstance(IInjector scope)
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

                IPool relatedPool = (IPool) scope.Get(typeof(IPool<>).MakeGenericType(Interface), PoolName);

                FInstance = relatedPool.Get();
            }

            State |= ServiceEntryStates.Built;

            return FInstance;
        }

        public override object GetSingleInstance() => FInstance ?? throw new InvalidOperationException(); // TODO: uzenet

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