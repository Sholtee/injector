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

    internal sealed class InstanceLifetime : InjectorDotNetLifetime, IConcreteLifetime<InstanceLifetime>
    {
        public InstanceLifetime(): base(bindTo: () => Instance, precedence: 40) {}

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, object value, bool externallyOwned)
        {
            yield return new InstanceServiceEntry(iface, name, value, externallyOwned, null);
        }
    }
}
