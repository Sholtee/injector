/********************************************************************************
* ServiceEnumerator.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceEnumerator<TInterface>: IEnumerable<TInterface> where TInterface: class
    {
        public ServiceEnumerator(IInstanceFactory scope, IServiceEntryResolver entryResolver)
        {
            Scope = scope;
            ServiceEntryResolver = entryResolver;
        }

        public IInstanceFactory Scope { get; }

        public IServiceEntryResolver ServiceEntryResolver { get; }

        public IEnumerator<TInterface> GetEnumerator()
        {
            foreach (AbstractServiceEntry entry in ServiceEntryResolver.ResolveMany(typeof(TInterface)))
            {
                yield return (TInterface) entry.ResolveInstance!(Scope);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
