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
        public ScopeFactory(IEnumerable<AbstractServiceEntry> entries) : base(entries)
        {
        }

        public virtual Injector_New CreateScope() => new(this);

        protected override IEnumerable<ContextualServiceEntry> ContextualServices
        {
            get 
            {
                yield return new ContextualServiceEntry(typeof(IInjector), owner => (IInjector) owner);
                yield return new ContextualServiceEntry(typeof(IScopeFactory), owner => (IScopeFactory) owner.Parent!);
            }
        }

        IInjector IScopeFactory.CreateScope() => CreateScope();

        IInjector IScopeFactory.CreateScope(IServiceContainer parent) => throw new NotImplementedException(); // TODO: torolni
    }
}
