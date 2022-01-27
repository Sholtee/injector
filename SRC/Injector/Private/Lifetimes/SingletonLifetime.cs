/********************************************************************************
* SingletonLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class SingletonLifetime : InjectorDotNetLifetime
    {
        public SingletonLifetime() : base(precedence: 30) => Singleton = this;

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation)
        {
            yield return new SingletonServiceEntry(iface, name, implementation, null);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs)
        {
            yield return new SingletonServiceEntry(iface, name, implementation, explicitArgs, null);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory)
        {
            yield return new SingletonServiceEntry(iface, name, factory, null);
        }

        public override string ToString() => nameof(Singleton);
    }
}
