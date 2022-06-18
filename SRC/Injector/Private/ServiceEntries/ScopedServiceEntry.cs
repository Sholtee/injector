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

    internal sealed class ScopedServiceEntry : ProducibleServiceEntry
    {
        public ScopedServiceEntry(Type @interface, string? name, Expression<Func<IInjector, Type, object>> factory) : base(@interface, name, factory)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation) : base(@interface, name, implementation)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs) : base(@interface, name, implementation, explicitArgs)
        {
        }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments!!) => this switch
        {
            _ when Implementation is not null && ExplicitArgs is null => new ScopedServiceEntry
            (
                Interface.MakeGenericType(genericArguments),
                Name,
                Implementation.MakeGenericType(genericArguments)
            ),
            _ when Implementation is not null && ExplicitArgs is not null => new ScopedServiceEntry
            (
                Interface.MakeGenericType(genericArguments),
                Name,
                Implementation.MakeGenericType(genericArguments),
                ExplicitArgs
            ),
            _ when Factory is not null => new ScopedServiceEntry
            (
                Interface.MakeGenericType(genericArguments),
                Name,
                Factory
            ),
            _ => throw new NotSupportedException()
        };

        public override Lifetime Lifetime { get; } = Lifetime.Scoped;

        public override ServiceEntryFlags Features { get; } = ServiceEntryFlags.CreateSingleInstance;
    }
}