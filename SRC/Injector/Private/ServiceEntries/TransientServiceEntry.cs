/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class TransientServiceEntry : ProducibleServiceEntry
    {
        public TransientServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory) : base(@interface, name, factory)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation) : base(@interface, name, implementation)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs) : base(@interface, name, implementation, explicitArgs)
        {
        }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
        {
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new TransientServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments)
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new TransientServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs
                ),
                _ when Factory is not null => new TransientServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override Lifetime Lifetime { get; } = Lifetime.Transient;
    }
}