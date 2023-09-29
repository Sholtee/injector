/********************************************************************************
* InstanceLifetime.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class InstanceLifetime : Lifetime
    {
        public InstanceLifetime() : base(precedence: 40) { }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, object? name, object value, ServiceOptions serviceOptions)
        {
            yield return new InstanceServiceEntry
            (
                iface ?? throw new ArgumentNullException(nameof(iface)),
                name,
                value ?? throw new ArgumentNullException(nameof(value)),
                serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions))
            );
        }

        public override string ToString() => nameof(Instance);
    }
}
