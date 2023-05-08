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

    //                                        !!!ATTENTION!!!
    //
    // This class is a critical component therefore every modification should be done carefully, with
    // performance in mind.
    // - NO Sysmte.Linq
    // - NO System.Reflection
    // - After ANY modifications, run the unit & performance tests to verify there is no regression
    //

    internal sealed class ServiceEnumerator<TInterface>: IEnumerable<TInterface> where TInterface: class
    {
        public ServiceEnumerator(IServiceActivator scope, IServiceResolver resolver)
        {
            Scope = scope;
            ServiceResolver = resolver;
        }

        public IServiceActivator Scope { get; }

        public IServiceResolver ServiceResolver { get; }

        public IEnumerator<TInterface> GetEnumerator()
        {
            foreach (AbstractServiceEntry entry in ServiceResolver.ResolveMany(typeof(TInterface)))
            {
                yield return (TInterface) Scope.GetOrCreateInstance(entry);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
