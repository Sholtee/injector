﻿/********************************************************************************
* ScopeFactorySupportsServiceProvider.cs                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ScopeFactorySupportsServiceProvider : ScopeFactory
    {
        public ScopeFactorySupportsServiceProvider(ISet<AbstractServiceEntry> entries, ScopeOptions options, CancellationToken cancellation = default) : base(entries, options, cancellation) { }

        public override Injector_New CreateScope() => new InjectorSupportsServiceProvider(this);

        protected new static IReadOnlyCollection<AbstractServiceEntry> DefaultBuiltInServices { get; } = ScopeFactory
            .DefaultBuiltInServices
            .Append(new ContextualServiceEntry(typeof(IServiceProvider), null, owner => (IServiceProvider)owner))
            .ToArray();

        protected override IReadOnlyCollection<AbstractServiceEntry> BuiltInServices { get; } = DefaultBuiltInServices;
    }
}
