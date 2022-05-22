/********************************************************************************
* SingletonLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class SingletonLifetime : InjectorDotNetLifetime
    {
        public SingletonLifetime() : base(precedence: 30) => Singleton = this;

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation)
        {
            yield return new SingletonServiceEntry(iface, name, implementation);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, object explicitArgs)
        {
            yield return new SingletonServiceEntry(iface, name, implementation, explicitArgs);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Expression<Func<IInjector, Type, object>> factory)
        {
            yield return new SingletonServiceEntry(iface, name, factory);
        }

        public override string ToString() => nameof(Singleton);
    }
}
