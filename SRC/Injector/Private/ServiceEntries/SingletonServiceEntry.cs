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
        public SingletonServiceEntry(Type type, object? key, Expression<FactoryDelegate> factory, ServiceOptions options) : base(type, key, factory, options)
        {
        }

        public SingletonServiceEntry(Type type, object? key, Type implementation, ServiceOptions options) : base(type, key, implementation, options)
        {
        }

        public SingletonServiceEntry(Type type, object? key, Type implementation, object explicitArgs, ServiceOptions options) : base(type, key, implementation, explicitArgs, options)
        {
        }

        public sealed override void Build(IBuildContext context, IReadOnlyList<IFactoryVisitor> visitors)
        {
            base.Build(context, visitors);
            AssignedSlot = context.AssignSlot();
         }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
        {
            if (genericArguments is null)
                throw new ArgumentNullException(nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new SingletonServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Implementation.MakeGenericType(genericArguments),
                    Options!
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new SingletonServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Options!
                ),
                _ when Factory is not null => new SingletonServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Factory,
                    Options!
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.CreateSingleInstance | ServiceEntryFeatures.Shared | ServiceEntryFeatures.SupportsBuild;

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Singleton;
    }
}