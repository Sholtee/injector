/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class InstanceServiceEntry : AbstractServiceEntry
    {
        private readonly object FInstance;

        public InstanceServiceEntry(Type @interface, string? name, object instance) : base(@interface, name, null)
        {
            Ensure.Parameter.IsNotNull(instance, nameof(instance));

            FInstance = instance;

            //
            // It will throw if the service interface is derocated with an aspect.
            //

            this.ApplyAspects();

            Flags = ServiceEntryFlags.Validated | ServiceEntryFlags.Shared | ServiceEntryFlags.CreateSingleInstance;
        }


        public override object CreateInstance(IInjector scope, out object? lifetime)
        {
            lifetime = null;
            return FInstance;
        }

        public override Lifetime? Lifetime { get; } = Lifetime.Instance;
    }
}