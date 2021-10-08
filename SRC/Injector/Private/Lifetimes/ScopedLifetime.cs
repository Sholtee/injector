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

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation)
        {
            yield return new ScopedServiceEntry(iface, name, implementation, null);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs)
        {
            yield return new ScopedServiceEntry(iface, name, implementation, explicitArgs, null);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory)
        {
            yield return new ScopedServiceEntry(iface, name, factory, null);
        }
    }
}
