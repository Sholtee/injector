/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class SingletonServiceEntry : ProducibleServiceEntry
    {
        private object? FInstance;

        private SingletonServiceEntry(SingletonServiceEntry entry, IServiceRegistry? owner) : base(entry, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceRegistry? owner) : base(@interface, name, factory, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, IServiceRegistry? owner) : base(@interface, name, implementation, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceRegistry? owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override object CreateInstance(IInjector scope)
        {
            Ensure.Parameter.IsNotNull(scope, nameof(scope));
            EnsureProducible();

            if (FInstance is not null)
                throw new InvalidOperationException(); // TODO: uzenet

            FInstance = Factory!(scope, Interface);

            UpdateState(ServiceEntryStates.Built);

            return FInstance;
        }

        public override object GetSingleInstance() => FInstance ?? throw new InvalidOperationException(); // TODO: uzenet

        public override AbstractServiceEntry Specialize(IServiceRegistry? owner, params Type[] genericArguments)
        {
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    owner
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    owner
                ),
                _ when Factory is not null => new SingletonServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    owner
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override AbstractServiceEntry CopyTo(IServiceRegistry registry) => new SingletonServiceEntry(this, Ensure.Parameter.IsNotNull(registry, nameof(registry)));

        public override Lifetime Lifetime { get; } = Lifetime.Singleton;

        public override bool IsShared { get; } = true;
    }
}