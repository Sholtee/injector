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

    internal class ScopeFactory : ServiceRegistry, IScopeFactory
    {
        public ScopeFactory(IEnumerable<AbstractServiceEntry> entries) : base(entries, Config.Value.ServiceContainer.MaxChildCount) // TODO: ServiceFactory config
        {
        }

        public virtual Injector_New CreateScope() => new(this);

        IInjector IScopeFactory.CreateScope() => CreateScope();

        IInjector IScopeFactory.CreateScope(IServiceContainer parent) => throw new NotImplementedException(); // TODO: torolni
    }
}
