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

    internal sealed class InstanceServiceEntry : SingletonServiceEntry
    {
        public InstanceServiceEntry(Type iface, string? name, object instance) : base(iface ?? throw new ArgumentNullException(nameof(iface)), name, (_, _) => instance)
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            //
            // It will throw if the service interface is derocated with an aspect.
            //

            if (Interface.GetCustomAttributes<AspectAttribute>(inherit: true).Any())
                throw new NotSupportedException(Resources.PROXYING_NOT_SUPPORTED);
        }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments) => throw new NotSupportedException();

        public override Lifetime Lifetime { get; } = Lifetime.Instance;
    }
}