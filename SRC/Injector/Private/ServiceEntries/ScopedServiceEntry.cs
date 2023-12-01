/********************************************************************************
* ScopedServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ScopedServiceEntry : ScopedServiceEntryBase
    {
        public ScopedServiceEntry(Type @interface, object? name, Expression<FactoryDelegate> factory, ServiceOptions options) : base(@interface, name, factory, options)
        {
        }

        public ScopedServiceEntry(Type @interface, object? name, Type implementation, ServiceOptions options) : base(@interface, name, implementation, options)
        {
        }

        public ScopedServiceEntry(Type @interface, object? name, Type implementation, object explicitArgs, ServiceOptions options) : base(@interface, name, implementation, explicitArgs, options)
        {
        }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
        {
            if (genericArguments is null)
                throw new ArgumentNullException(nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new ScopedServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Implementation.MakeGenericType(genericArguments),
                    Options!
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new ScopedServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Options!
                ),
                _ when Factory is not null => new ScopedServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Factory,
                    Options!
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Scoped;

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.SupportsBuild;
    }
}