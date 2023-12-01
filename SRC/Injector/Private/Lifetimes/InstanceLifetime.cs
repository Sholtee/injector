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

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, object value, ServiceOptions serviceOptions)
        {
            yield return new InstanceServiceEntry
            (
                type ?? throw new ArgumentNullException(nameof(type)),
                key,
                value ?? throw new ArgumentNullException(nameof(value)),
                serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions))
            );
        }

        public override string ToString() => nameof(Instance);
    }
}
