/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal sealed class TransientServiceEntry : ProducibleServiceEntry
    {
        private int FInstanceCount;

        private TransientServiceEntry(TransientServiceEntry entry, IServiceRegistry owner) : base(entry, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceRegistry? owner) : base(@interface, name, factory, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, IServiceRegistry? owner) : base(@interface, name, implementation, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, IServiceRegistry? owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override object GetSingleInstance() => throw new NotSupportedException();

        public override object CreateInstance(IInjector scope)
        {
            Ensure.Parameter.IsNotNull(scope, nameof(scope));
            EnsureProducible();

            if (FInstanceCount == scope.Options.MaxSpawnedTransientServices)
                //
                // Ha ide jutunk az azt jelenti h jo esellyel a tartalmazo injector ujrahasznositasra kerult
                // (ahogy az a teljesitmeny teszteknel meg is tortent).
                //

                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.INJECTOR_SHOULD_BE_RELEASED, scope.Options.MaxSpawnedTransientServices));

            object instance = Factory!(scope, Interface);

            UpdateState(ServiceEntryStates.Instantiated);
            FInstanceCount++;

            return instance;
        }

        public override AbstractServiceEntry CopyTo(IServiceRegistry owner) => new TransientServiceEntry(this, Ensure.Parameter.IsNotNull(owner, nameof(owner)));

        public override AbstractServiceEntry Specialize(IServiceRegistry? owner, params Type[] genericArguments)
        {
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return this switch
            {
                _ when Implementation is not null && ExplicitArgs is null => new TransientServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    owner
                ),
                _ when Implementation is not null && ExplicitArgs is not null => new TransientServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Implementation.MakeGenericType(genericArguments),
                    ExplicitArgs,
                    owner
                ),
                _ when Factory is not null => new TransientServiceEntry
                (
                    Interface.MakeGenericType(genericArguments),
                    Name,
                    Factory,
                    owner
                ),
                _ => throw new NotSupportedException()
            };
        }

        public override Lifetime Lifetime { get; } = Lifetime.Transient;
    }
}