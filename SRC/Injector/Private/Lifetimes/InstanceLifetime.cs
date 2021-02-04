/********************************************************************************
* InstanceLifetime.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class InstanceLifetime : Lifetime
    {
        [ModuleInitializer]
        public static void Setup() => Instance = new InstanceLifetime();

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, object value, bool externallyOwned, IServiceContainer owner, params Func<object, Type, object>[] customConverters)
        {
            yield return new InstanceServiceEntry(iface, name, value, externallyOwned, owner, customConverters);
        }

        public override bool IsCompatible(AbstractServiceEntry entry) => entry is InstanceServiceEntry;

        public override string ToString() => nameof(Instance);
    }
}
