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
        public PooledServiceEntry(Type @interface, string? name, Expression<FactoryDelegate> factory, ServiceOptions options, string poolName) : base(@interface, name, factory, options) =>
            PoolName = poolName;

        public PooledServiceEntry(Type @interface, string? name, Type implementation, ServiceOptions options, string poolName) : base(@interface, name, implementation, options) =>
            PoolName = poolName;

        public PooledServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, ServiceOptions options, string poolName) : base(@interface, name, implementation, explicitArgs, options) =>
            PoolName = poolName;

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
                    Options!,
                    PoolName
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Options!,
                    PoolName
                ),
                _ when Factory is not null => new PooledServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    Options!,
                    PoolName
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Pooled;

        public string PoolName { get; }

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.SupportsBuild;
    }
}