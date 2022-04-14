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

    internal sealed class InstanceLifetime : InjectorDotNetLifetime
    {
        public InstanceLifetime() : base(precedence: 40) => Instance = this;

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, object value)
        {
            yield return new InstanceServiceEntry(iface, name, value);
        }

        public override string ToString() => nameof(Instance);
    }
}
