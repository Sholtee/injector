/********************************************************************************
* ScopedLifetime.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ScopedLifetime : InjectorDotNetLifetime, IConcreteLifetime<ScopedLifetime>
    {
        public ScopedLifetime() : base(bindTo: () => Scoped, precedence: 10) { }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner)
        {
            yield return new ScopedServiceEntry(iface, name, implementation, owner);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner)
        {
            yield return new ScopedServiceEntry(iface, name, implementation, explicitArgs, owner);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner)
        {
            yield return new ScopedServiceEntry(iface, name, factory, owner);
        }
    }
}
