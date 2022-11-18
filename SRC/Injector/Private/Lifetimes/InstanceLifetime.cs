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

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, object value)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (value is null)
                throw new ArgumentNullException(nameof(value));

            yield return new InstanceServiceEntry(iface, name, value);
        }

        public override string ToString() => nameof(Instance);
    }
}
