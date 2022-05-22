/********************************************************************************
* TransientLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class TransientLifetime : InjectorDotNetLifetime
    {
        public TransientLifetime() : base(precedence: 10) => Transient = this;

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation)
        {
            yield return new TransientServiceEntry(iface, name, implementation);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, object explicitArgs)
        {
             yield return new TransientServiceEntry(iface, name, implementation, explicitArgs);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Expression<Func<IInjector, Type, object>> factory)
        {
            yield return new TransientServiceEntry(iface, name, factory);
        }

        public override string ToString() => nameof(Transient);
    }
}
