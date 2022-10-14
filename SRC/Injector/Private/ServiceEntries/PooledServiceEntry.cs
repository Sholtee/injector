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

    internal sealed partial class PooledServiceEntry : ScopedServiceEntryBase
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

        public override ServiceEntryFeatures Features { get; } = ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.SupportsBuild;
    }
}