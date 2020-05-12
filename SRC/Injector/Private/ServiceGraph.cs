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
        private readonly Stack<ServiceReference> FGraph;

        private ServiceGraph(ServiceGraph parent)
        {
            FGraph = new Stack<ServiceReference>(parent);
            
            Debug.Assert(Current == parent.Current);
        }

        public ServiceGraph() => FGraph = new Stack<ServiceReference>();

        public ServiceReference? Current => FGraph.Any() ? FGraph.Peek() : null;

        public void CheckNotCircular()
        {
            //
            // Ha egynel tobbszor szerepel az aktualis szerviz az aktualis utvonalon akkor korkoros referenciank van.
            //

            int firstIndex = this.FirstIndexOf(Ensure.IsNotNull(Current, nameof(Current)), ServiceReferenceComparer.Instance);

            if (firstIndex < FGraph.Count - 1)
                throw new CircularReferenceException(this
                    //
                    // Csak magat a kort adjuk vissza.
                    //

                    .Skip(firstIndex)
                    .Select(sr => sr.RelatedServiceEntry));
        }

        public IDisposable With(ServiceReference node) 
        {
            FGraph.Push(node);
            return new WithScope(FGraph);
        }

        public ServiceGraph CreateSubgraph() => new ServiceGraph(this);

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

        public IEnumerator<ServiceReference> GetEnumerator() =>
            //
            // Verem elemek felsorolasa forditva tortenik -> Reverse()
            //

            FGraph.Reverse().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
