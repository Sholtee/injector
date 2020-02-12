/********************************************************************************
* ServiceGraph.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceGraph: IEnumerable<ServiceReference>
    {
        private readonly Stack<ServiceReference> FGraph = new Stack<ServiceReference>();

        public ServiceReference Current => FGraph.Any() ? FGraph.Peek() : null;

        public bool CircularReference =>
            //
            // Ha egynel tobbszor szerepel az aktualis szerviz akkor korkoros referenciank van.
            //

            Current != null && FGraph.LastIndexOf(Current, ServiceReferenceComparer.Instance) > 0;

        public void Add(ServiceReference svc) =>
            Current?.Dependencies.Add(svc);

        public IDisposable With(ServiceReference svc) 
        {
            FGraph.Push(svc);
            return new WithScope(FGraph);
        }

        private sealed class WithScope : Disposable 
        {
            private readonly Stack<ServiceReference> FGraph;

            public WithScope(Stack<ServiceReference> graph) => FGraph = graph;

            protected override void Dispose(bool disposeManaged)
            {
                FGraph.Pop();
                base.Dispose(disposeManaged);
            }
        }

        IEnumerator<ServiceReference> IEnumerable<ServiceReference>.GetEnumerator() => FGraph.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FGraph.GetEnumerator();
    }
}
