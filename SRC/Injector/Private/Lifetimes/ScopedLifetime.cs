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

    internal sealed class ScopedLifetime : InjectorDotNetLifetime
    {
        public ScopedLifetime() : base(precedence: 10) => Scoped = this;

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation)
        {
            yield return new ScopedServiceEntry(iface, name, implementation);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, object explicitArgs)
        {
            yield return new ScopedServiceEntry(iface, name, implementation, explicitArgs);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory)
        {
            yield return new ScopedServiceEntry(iface, name, factory);
        }

        public override string ToString() => nameof(Scoped);
    }
}
