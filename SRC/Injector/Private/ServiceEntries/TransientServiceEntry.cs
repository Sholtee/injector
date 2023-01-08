/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class TransientServiceEntry : ProducibleServiceEntry
    {
        public TransientServiceEntry(Type @interface, string? name, Expression<FactoryDelegate> factory, bool supportAspects) : base(@interface, name, factory, supportAspects)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, bool supportAspects) : base(@interface, name, implementation, supportAspects)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, bool supportAspects) : base(@interface, name, implementation, explicitArgs, supportAspects)
        {
        }

        public override void Build(IBuildContext? context, params IFactoryVisitor[] visitors)
        {
            base.Build(context, visitors);

            if (context is not null)
                ResolveInstance = (IInstanceFactory factory) =>
                {
                    //
                    // Inlining works against non-interface, non-virtual methods only
                    //

                    if (factory is Injector injector)
                        return injector.GetOrCreateInstance(this, null);

                    return factory.GetOrCreateInstance(this, null);
                };
        }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
        {
            if (genericArguments is null)
                throw new ArgumentNullException(nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new TransientServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    Features.HasFlag(ServiceEntryFeatures.SupportsAspects)
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new TransientServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Features.HasFlag(ServiceEntryFeatures.SupportsAspects)
                ),
                _ when Factory is not null => new TransientServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    Features.HasFlag(ServiceEntryFeatures.SupportsAspects)
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Transient;

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.SupportsBuild;
    }
}