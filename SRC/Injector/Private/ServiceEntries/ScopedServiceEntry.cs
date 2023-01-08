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
        public ScopedServiceEntry(Type @interface, string? name, Expression<FactoryDelegate> factory, bool supportAspects) : base(@interface, name, factory, supportAspects)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation, bool supportAspects) : base(@interface, name, implementation, supportAspects)
        {
        }

        public ScopedServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, bool supportAspects) : base(@interface, name, implementation, explicitArgs, supportAspects)
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
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    Features.HasFlag(ServiceEntryFeatures.SupportsAspects)
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new ScopedServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Features.HasFlag(ServiceEntryFeatures.SupportsAspects)
                ),
                _ when Factory is not null => new ScopedServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    Features.HasFlag(ServiceEntryFeatures.SupportsAspects)
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Scoped;

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.SupportsBuild;
    }
}