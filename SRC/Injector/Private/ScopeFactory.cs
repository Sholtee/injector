/********************************************************************************
* ScopeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ScopeFactory : ConcurrentServiceRegistry, IScopeFactory
    {
        public ScopeFactory(ISet<AbstractServiceEntry> entries) : base(entries)
        {
        }

        public virtual Injector_New CreateScope() => new(this);

        protected override IReadOnlyCollection<AbstractServiceEntry> BuiltInServices => new AbstractServiceEntry[]
        {
            new ContextualServiceEntry(typeof(IInjector), owner => (IInjector) owner),
            new ContextualServiceEntry(typeof(IScopeFactory), owner => (IScopeFactory) owner.Parent!),
            new ScopedServiceEntry(typeof(IEnumerable<>), null, typeof(ServiceEnumerator<>), null!)
        };

        IInjector IScopeFactory.CreateScope() => CreateScope();

        IInjector IScopeFactory.CreateScope(IServiceContainer parent) => throw new NotImplementedException(); // TODO: torolni
    }
}
