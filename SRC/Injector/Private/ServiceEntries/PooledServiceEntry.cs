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

    internal sealed partial class PooledServiceEntry : ScopedServiceEntryBase
    {
        public const string POOL_SCOPE = nameof(POOL_SCOPE);

        private Type? FPoolType;
        public Type PoolType =>
            FPoolType ??= typeof(IPool<>).MakeGenericType(Interface);

        private string? FPoolName;
        public string PoolName => 
            FPoolName ??= $"{IServiceCollection.Consts.INTERNAL_SERVICE_NAME_PREFIX}pool_{(Interface.IsConstructedGenericType ? Interface.GetGenericTypeDefinition() : Interface, Name).GetHashCode():X}";

        public PooledServiceEntry(Type @interface, string? name, Expression<FactoryDelegate> factory, ServiceOptions options) : base(@interface, name, factory, options)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, ServiceOptions options) : base(@interface, name, implementation, options)
        {
        }

        public PooledServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, ServiceOptions options) : base(@interface, name, implementation, explicitArgs, options)
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
                    Options
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Options
                ),
                _ when Factory is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    Options
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Pooled;

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.SupportsBuild;
    }
}