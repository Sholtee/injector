/********************************************************************************
* ScopeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ScopeFactory : ConcurrentServiceRegistry, IScopeFactory
    {
        public ScopeFactory(ISet<AbstractServiceEntry> entries, ScopeOptions scopeOptions, CancellationToken cancellation = default) : base(entries, cancellation: cancellation)
        {
            ScopeOptions = scopeOptions;
        }

        public ScopeOptions ScopeOptions { get; }

        public virtual Injector CreateScope(bool register) => new Injector(this, register);

        protected static IReadOnlyCollection<AbstractServiceEntry> DefaultBuiltInServices { get; } = new AbstractServiceEntry[]
        {
            new ContextualServiceEntry(typeof(IServiceRegistry), null, owner => owner),
            new ContextualServiceEntry(typeof(IInjector), null,  owner => (IInjector) owner),
            new ContextualServiceEntry(typeof(IScopeFactory), null, owner => (IScopeFactory) owner.Parent!),
            new ContextualServiceEntry(typeof(IDictionary<string, object?>), IInjectorBasicExtensions.META_NAME, _ => new Dictionary<string, object?>()), // ne Scoped legyen h StrictDI ne anyazzon
            new ScopedServiceEntry(typeof(IEnumerable<>), null, typeof(ServiceEnumerator<>), null!)
        };

        protected override IReadOnlyCollection<AbstractServiceEntry> BuiltInServices { get; } = DefaultBuiltInServices;

        IInjector IScopeFactory.CreateScope() => CreateScope(ScopeOptions.SafeMode);

        IInjector IScopeFactory.CreateScopeSafe() => CreateScope(true);
    }
}
