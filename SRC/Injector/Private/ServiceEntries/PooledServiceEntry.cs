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

    internal sealed partial class PooledServiceEntry : ScopedServiceEntryBase
    {
        public PooledServiceEntry(Type type, object? key, Expression<FactoryDelegate> factory, ServiceOptions options, object poolId) : base(type, key, factory, options) =>
            PoolId = poolId;

        public PooledServiceEntry(Type type, object? key, Type implementation, ServiceOptions options, object poolId) : base(type, key, implementation, options) =>
            PoolId = poolId;

        public PooledServiceEntry(Type type, object? key, Type implementation, object explicitArgs, ServiceOptions options, object poolId) : base(type, key, implementation, explicitArgs, options) =>
            PoolId = poolId;

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
        {
            if (genericArguments is null)
                throw new ArgumentNullException(nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new PooledServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Implementation.MakeGenericType(genericArguments),
                    Options!,
                    PoolId
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Options!,
                    PoolId
                ),
                _ when Factory is not null => new PooledServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Factory,
                    Options!,
                    PoolId
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Pooled;

        public object PoolId { get; }

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.SupportsBuild;
    }
}