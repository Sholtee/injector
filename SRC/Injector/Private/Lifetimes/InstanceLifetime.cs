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

    internal sealed class InstanceLifetime : InjectorDotNetLifetime<InstanceLifetime>
    {
        public InstanceLifetime(): base(bindTo: () => Instance, precedence: 40) {}

        [ModuleInitializer]
        public static void Setup() => Bind();

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, object value, bool externallyOwned, IServiceContainer owner, params Func<object, Type, object>[] customConverters)
        {
            yield return new InstanceServiceEntry(iface, name, value, externallyOwned, owner, customConverters);
        }
    }
}
