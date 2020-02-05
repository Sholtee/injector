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
    internal sealed class ServiceGraph: IEnumerable<AbstractServiceReference>
    {
        private readonly Stack<AbstractServiceReference> FGraph = new Stack<AbstractServiceReference>();

        public AbstractServiceReference Current => FGraph.Any() ? FGraph.Peek() : null;

        public bool CircularReference =>
            //
            // Ha egynel tobbszor szerepel az aktualis szerviz akkor korkoros referenciank van.
            //

            Current != null && FGraph.LastIndexOf(Current, ServiceReferenceComparer.Instance) > 0;

        public void Add(AbstractServiceReference svc) =>
            Current?.Dependencies.Add(svc);

        public IDisposable With(AbstractServiceReference svc) 
        {
            FGraph.Push(svc);
            return new WithScope(FGraph);
        }

        private sealed class WithScope : Disposable 
        {
            private readonly Stack<AbstractServiceReference> FGraph;

            public WithScope(Stack<AbstractServiceReference> graph) => FGraph = graph;

            protected override void Dispose(bool disposeManaged)
            {
                FGraph.Pop();
                base.Dispose(disposeManaged);
            }
        }

        IEnumerator<AbstractServiceReference> IEnumerable<AbstractServiceReference>.GetEnumerator() => FGraph.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FGraph.GetEnumerator();
    }
}
