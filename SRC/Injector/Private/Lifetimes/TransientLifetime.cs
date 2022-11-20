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

    internal sealed class TransientLifetime : Lifetime
    {
        public TransientLifetime() : base(precedence: 10) { }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            yield return new TransientServiceEntry(iface, name, implementation);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, object explicitArgs)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            if (explicitArgs is null)
                throw new ArgumentNullException(nameof(explicitArgs));

            yield return new TransientServiceEntry(iface, name, implementation, explicitArgs);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Expression<FactoryDelegate> factory)
        {
            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            yield return new TransientServiceEntry(iface, name, factory);
        }

        public override string ToString() => nameof(Transient);
    }
}
