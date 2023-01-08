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

    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        public SingletonServiceEntry(Type @interface, string? name, Expression<FactoryDelegate> factory, bool supportAspects) : base(@interface, name, factory, supportAspects)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, bool supportAspects) : base(@interface, name, implementation, supportAspects)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, bool supportAspects) : base(@interface, name, implementation, explicitArgs, supportAspects)
        {
        }

        public sealed override void Build(IBuildContext? context, params IFactoryVisitor[] visitors)
        {
            base.Build(context, visitors);

            if (context is null)
                return;

            int assignedSlot = context.AssignSlot();

            ResolveInstance = (IInstanceFactory factory) =>
            {
                //
                // Inlining works against non-interface, non-virtual methods only
                //

                if (factory is Injector injector)
                {
                    if (injector.Super is not null)
                        injector = (Injector)injector.Super;

                    return injector.GetOrCreateInstance(this, assignedSlot);
                }

                if (factory.Super is not null)
                    factory = factory.Super;

                return factory.GetOrCreateInstance(this, assignedSlot);
            };
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
                    Implementation.MakeGenericType(genericArguments),
                    Features.HasFlag(ServiceEntryFeatures.SupportsAspects)
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Features.HasFlag(ServiceEntryFeatures.SupportsAspects)
                ),
                _ when Factory is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    Features.HasFlag(ServiceEntryFeatures.SupportsAspects)
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.Shared | ServiceEntryFeatures.SupportsBuild;

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Singleton;
    }
}