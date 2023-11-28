﻿/********************************************************************************
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

    internal sealed class SingletonLifetime : Lifetime
    {
        public SingletonLifetime() : base(precedence: 30) { }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, Type implementation, ServiceOptions serviceOptions)
        {
            yield return new SingletonServiceEntry
            (
                type ?? throw new ArgumentNullException(nameof(type)),
                key,
                implementation ?? throw new ArgumentNullException(nameof(implementation)),
                serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions))
            );
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, Type implementation, object explicitArgs, ServiceOptions serviceOptions)
        {
            yield return new SingletonServiceEntry
            (
                type ?? throw new ArgumentNullException(nameof(type)),
                key,
                implementation ?? throw new ArgumentNullException(nameof(implementation)),
                explicitArgs ?? throw new ArgumentNullException(nameof(explicitArgs)),
                serviceOptions ?? throw new ArgumentNullException(nameof(explicitArgs))
            );
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, Expression<FactoryDelegate> factory, ServiceOptions serviceOptions)
        {
            yield return new SingletonServiceEntry
            (
                type ?? throw new ArgumentNullException(nameof(type)),
                key,
                factory ?? throw new ArgumentNullException(nameof(factory)),
                serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions))
            );
        }

        public override string ToString() => nameof(Singleton);
    }
}
