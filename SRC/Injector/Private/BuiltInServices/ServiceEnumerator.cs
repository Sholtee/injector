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
        public ServiceEnumerator(IInstanceFactory scope, IServiceResolver resolver)
        {
            Scope = scope;
            ServiceResolver = resolver;
        }

        public IInstanceFactory Scope { get; }

        public IServiceResolver ServiceResolver { get; }

        public IEnumerator<TInterface> GetEnumerator()
        {
            foreach (AbstractServiceEntry entry in ServiceResolver.ResolveMany(typeof(TInterface)))
            {
                yield return (TInterface) entry.ResolveInstance!(Scope);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
