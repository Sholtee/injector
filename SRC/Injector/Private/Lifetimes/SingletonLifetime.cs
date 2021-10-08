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

    internal sealed class SingletonLifetime : InjectorDotNetLifetime, IConcreteLifetime<SingletonLifetime>
    {
        public SingletonLifetime() : base(bindTo: () => Singleton, precedence: 30) { }

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
    }
}
