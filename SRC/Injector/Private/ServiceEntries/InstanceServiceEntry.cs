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

    internal sealed class InstanceServiceEntry : SingletonServiceEntryBase
    {
        public object Instance { get; }

        public InstanceServiceEntry(Type iface, string? name, object instance) : base(iface ?? throw new ArgumentNullException(nameof(iface)), name)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));

            //
            // It will throw if the service interface is derocated with an aspect.
            //

            if (Interface.GetCustomAttributes<AspectAttribute>(inherit: true).Any())
                throw new NotSupportedException(Resources.PROXYING_NOT_SUPPORTED);

            State = ServiceEntryStates.Validated | ServiceEntryStates.Built;
            CreateInstance = (IInstanceFactory scope, out object? lifetime) =>
            {
                lifetime = null;
                return Instance;
            };
        }

        public override Lifetime? Lifetime { get; } = Lifetime.Instance;

        public override ServiceEntryFeatures Features { get; } = ServiceEntryFeatures.Shared | ServiceEntryFeatures.CreateSingleInstance;
    }
}