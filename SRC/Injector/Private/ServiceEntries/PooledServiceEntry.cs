/********************************************************************************
* PooledServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Primitives.Threading;

    internal sealed class PooledServiceEntry : ProducibleServiceEntry
    {
        private Type? FPoolType;
        public Type PoolType => FPoolType ??= typeof(IPool<>).MakeGenericType(Interface);

        public PooledServiceEntry(Type @interface, string? name, Expression<Func<IInjector, Type, object>> factory, string poolName) : base(@interface, name, factory)
        {
            PoolName = poolName;
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, string poolName) : base(@interface, name, implementation)
        {
            PoolName = poolName;
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, string poolName) : base(@interface, name, implementation, explicitArgs)
        {
            PoolName = poolName;
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

        public override object CreateInstance(IInjector scope, out object? lifetime)
        {
            if (scope.Lifetime is ILifetimeManager<object>)
                //
                // In pool, we call the original factory
                //

                return base.CreateInstance(scope, out lifetime);
            else
            {
                //
                // On consumer side we get the item from the pool
                //

                IPool relatedPool = (IPool) scope.Get(PoolType, PoolName);

                object result = relatedPool.Get();
                lifetime = new PoolItemCheckin(relatedPool, result);
                return result;
            }
        }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
        {
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    PoolName
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    PoolName
                ),
                _ when Factory is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    PoolName
                ),
                _ => throw new NotSupportedException()
            };
        }

        public string PoolName { get; }

        public override Lifetime Lifetime { get; } = Lifetime.Pooled;

        public override ServiceEntryFlags Features { get; } = ServiceEntryFlags.CreateSingleInstance;
    }
}