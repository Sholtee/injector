/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    internal sealed class InstanceServiceEntry : AbstractServiceEntry
    {
        public object Instance { get; }

        public InstanceServiceEntry(Type @interface, string? name, object instance) : base(@interface, name, null, null)
        {
            Ensure.Parameter.IsNotNull(instance, nameof(instance));

            Instance = instance;

            //
            // It will throw if the service interface is derocated with an aspect.
            //

            if (Interface.GetCustomAttributes<AspectAttribute>(inherit: true).Any())
                throw new NotSupportedException(Resources.PROXYING_NOT_SUPPORTED);

            State = ServiceEntryStateFlags.Validated | ServiceEntryStateFlags.Built;
        }

        public override object CreateInstance(IInjector scope, out object? lifetime)
        {
            lifetime = null;
            return Instance;
        }

        public override Lifetime? Lifetime { get; } = Lifetime.Instance;

        public override ServiceEntryFlags Features { get; } = ServiceEntryFlags.Shared | ServiceEntryFlags.CreateSingleInstance;
    }
}