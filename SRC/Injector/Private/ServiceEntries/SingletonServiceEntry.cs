/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        public SingletonServiceEntry(Type @interface, string? name, Expression<FactoryDelegate> factory, ServiceOptions options) : base(@interface, name, factory, options)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, ServiceOptions options) : base(@interface, name, implementation, options)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, ServiceOptions options) : base(@interface, name, implementation, explicitArgs, options)
        {
        }

        public sealed override void Build(IBuildContext context, IReadOnlyList<IFactoryVisitor> visitors)
        {
            base.Build(context, visitors);

            int assignedSlot = context.AssignSlot();

            ResolveInstance = (IInstanceFactory factory) => (factory.Super ?? factory).GetOrCreateInstance(this, assignedSlot);
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
                    Options
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Options
                ),
                _ when Factory is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    Options
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.Shared | ServiceEntryFeatures.SupportsBuild;

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Singleton;
    }
}