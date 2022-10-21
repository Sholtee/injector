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

    internal sealed class PooledServiceEntry : ScopedServiceEntryBase
    {
        private Type? FPoolType;
        public Type PoolType =>
            FPoolType ??= typeof(IPool<>).MakeGenericType(Interface);

        private string? FPoolName;
        public string PoolName => 
            FPoolName ??= $"{Consts.INTERNAL_SERVICE_NAME_PREFIX}pool_{(Interface.IsConstructedGenericType ? Interface.GetGenericTypeDefinition() : Interface, Name).GetHashCode():X}";

        public PooledServiceEntry(Type @interface, string? name, Expression<Func<IInjector, Type, object>> factory) : base(@interface, name, factory)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation) : base(@interface, name, implementation)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs) : base(@interface, name, implementation, explicitArgs)
        {
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
            if (scope.Tag is ILifetimeManager<object>)
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
            if (genericArguments is null)
                throw new ArgumentNullException(nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments)
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs
                ),
                _ when Factory is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override Lifetime Lifetime { get; } = Lifetime.Pooled;

        public override ServiceEntryFeatures Features { get; } = ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.SupportsVisit;
    }
}