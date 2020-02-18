/********************************************************************************
* ServiceGraph.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceGraph: IEnumerable<ServiceReference>
    {
        private readonly Stack<ServiceReference> FGraph;

        private ServiceGraph(ServiceGraph parent)
        {
            //
            // Verem elemek felsorolasa forditva tortenik -> Reverse()
            //

            FGraph = new Stack<ServiceReference>(parent.Reverse());
            
            Assert(Current == parent.Current);
        }

        public ServiceGraph() => FGraph = new Stack<ServiceReference>();

        public ServiceReference Current => FGraph.Any() ? FGraph.Peek() : null;

        public void CheckNotCircular()
        {
            Assert(Current != null);

            //
            // - Ha egynel tobbszor szerepel az aktualis szerviz az aktualis utvonalon akkor korkoros referenciank van.
            // - Mivel a verem elemek felsorolasa visszafele tortenik az utoljara hozzaadott elem indexe 0 lesz.
            //

            int lastIndex = this.LastIndexOf(Current, ServiceReferenceComparer.Instance);

            if (lastIndex > 0)
                throw new CircularReferenceException(this
                    //
                    // Csak magat a kort adjuk vissza.
                    //

                    .Take(lastIndex + 1)
                    .Reverse()
                    .Select(sr => sr.RelatedServiceEntry));
        }

        public void AddAsDependency(ServiceReference node)
        {
            Assert(Current != node);

            Current?.Dependencies.Add(node);
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
                FGraph.Pop().Release();
                base.Dispose(disposeManaged);
            }
        }

        IEnumerator<ServiceReference> IEnumerable<ServiceReference>.GetEnumerator() => FGraph.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FGraph.GetEnumerator();
    }
}
