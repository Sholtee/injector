/********************************************************************************
* ExperimentalScope.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ExperimentalScopeSupportsServiceProvider : ExperimentalScope, IServiceProvider
    {
        protected override IEnumerable<AbstractServiceEntry> GetAllServices(IEnumerable<AbstractServiceEntry> registeredEntries)
        {
            yield return new ContextualServiceEntry(typeof(IServiceProvider), null, (i, _) => i);

            foreach (AbstractServiceEntry entry in base.GetAllServices(registeredEntries))
            {
                yield return entry;
            }
        }

        public ExperimentalScopeSupportsServiceProvider(IEnumerable<AbstractServiceEntry> registeredEntries, ScopeOptions options, object? lifetime) : base(registeredEntries, options, lifetime)
        {
        }

        public ExperimentalScopeSupportsServiceProvider(ExperimentalScopeSupportsServiceProvider super, object? lifetime) : base(super, lifetime)
        {
        }

        public override object Get(Type iface, string? name) => TryGet(iface, name)!;

        public object GetService(Type serviceType) => TryGet(serviceType, null)!;

        public override IInjector CreateScope(object? lifetime = null) => new ExperimentalScopeSupportsServiceProvider(this, lifetime);
    }
}
