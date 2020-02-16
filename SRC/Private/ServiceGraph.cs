/********************************************************************************
* ServiceGraph.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceGraph: IEnumerable<ServiceReference>
    {
        private readonly Stack<ServiceReference> FGraph = new Stack<ServiceReference>();

        public ServiceReference Current => FGraph.Any() ? FGraph.Peek() : null;

        public void CheckNotCircular()
        {
            Debug.Assert(Current != null);

            //
            // Ha egynel tobbszor szerepel az aktualis szerviz az aktualis utvonalon akkor korkoros referenciank van.
            //

            if (this.LastIndexOf(Current, ServiceReferenceComparer.Instance) > 0)
                throw new CircularReferenceException(this.Select(sr => sr.RelatedServiceEntry));
        }

        public void AddAsDependency(ServiceReference node)
        {
            Debug.Assert(Current != node);

            Current?.Dependencies.Add(node);
        }

        public IDisposable With(ServiceReference node) 
        {
            FGraph.Push(node);
            return new WithScope(FGraph);
        }

        private sealed class WithScope : Disposable 
        {
            private readonly Stack<ServiceReference> FGraph;

            public WithScope(Stack<ServiceReference> graph) => FGraph = graph;

            protected override void Dispose(bool disposeManaged)
            {
                FGraph.Pop().Release();
                base.Dispose(disposeManaged);
            }
        }

        IEnumerator<ServiceReference> IEnumerable<ServiceReference>.GetEnumerator() => FGraph.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FGraph.GetEnumerator();
    }
}
