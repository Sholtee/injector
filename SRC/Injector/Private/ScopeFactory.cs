/********************************************************************************
* ScopeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ScopeFactory : ConcurrentServiceRegistry, IScopeFactory
    {
        public ScopeFactory(ISet<AbstractServiceEntry> entries, ScopeOptions? options = null, CancellationToken cancellation = default) : base(entries, cancellation: cancellation)
        {
            Options = options ?? new ScopeOptions();
        }

        public ScopeOptions Options { get; }

        public virtual Injector_New CreateScope() => new(this);

        protected override IReadOnlyCollection<AbstractServiceEntry> BuiltInServices => new AbstractServiceEntry[]
        {
            new ContextualServiceEntry(typeof(IServiceRegistry), null, owner => owner),
            new ContextualServiceEntry(typeof(IInjector), null,  owner => (IInjector) owner),
            new ContextualServiceEntry(typeof(IScopeFactory), null, owner => (IScopeFactory) owner.Parent!),
            new ContextualServiceEntry(typeof(IDictionary<string, object?>), IInjectorBasicExtensions.META_NAME, _ => new Dictionary<string, object?>()), // ne Scoped legyen h StrictDI sose anyazzon
            new ScopedServiceEntry(typeof(IEnumerable<>), null, typeof(ServiceEnumerator<>), null!)
        };

        IInjector IScopeFactory.CreateScope() => CreateScope();

        IInjector IScopeFactory.CreateScope(IServiceContainer parent) => throw new NotImplementedException(); // TODO: torolni
    }
}
