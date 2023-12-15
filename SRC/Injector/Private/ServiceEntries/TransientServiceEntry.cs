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
        public TransientServiceEntry(Type type, object? key, Expression<FactoryDelegate> factory, ServiceOptions options) : base(type, key, factory, options)
        {
        }

        public TransientServiceEntry(Type type, object? key, Type implementation, ServiceOptions options) : base(type, key, implementation, options)
        {
        }

        public TransientServiceEntry(Type type, object? key, Type implementation, object explicitArgs, ServiceOptions options) : base(type, key, implementation, explicitArgs, options)
        {
        }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
        {
            if (genericArguments is null)
                throw new ArgumentNullException(nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new TransientServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Implementation.MakeGenericType(genericArguments),
                    Options!
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new TransientServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    Options!
                ),
                _ when Factory is not null => new TransientServiceEntry
                (
                    Type.MakeGenericType(genericArguments),
                    Key,
                    Factory,
                    Options!
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Transient;

        public override ServiceEntryFeatures Features => base.Features | ServiceEntryFeatures.SupportsBuild;
    }
}