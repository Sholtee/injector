/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class SingletonServiceEntry : SingletonServiceEntryBase
    {
        public SingletonServiceEntry(Type @interface, string? name, Expression<Func<IInjector, Type, object>> factory) : base(@interface, name, factory)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation) : base(@interface, name, implementation)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs) : base(@interface, name, implementation, explicitArgs)
        {
        }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
        {
            if (genericArguments is null)
                throw new ArgumentNullException(nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments)
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs
                ),
                _ when Factory is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Singleton;

        public override ServiceEntryFeatures Features { get; } = ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.Shared | ServiceEntryFeatures.SupportsBuild;
    }
}