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
    using Interfaces;
    using Primitives.Patterns;
    using Properties;

    internal sealed class ServiceGraph: IServiceGraph
    {
        private readonly Stack<IServiceReference> FGraph = new();

        public IServiceReference? Requestor => FGraph.Any() ? FGraph.Peek() : null;

        public void CheckNotCircular()
        {
            //
            // Ha egynel tobbszor szerepel az aktualis szerviz az aktualis utvonalon akkor korkoros referenciank van.
            //

            int firstIndex = this.FirstIndexOf(Ensure.IsNotNull(Requestor, nameof(Requestor)), ServiceReferenceComparer.Instance);

            if (firstIndex < FGraph.Count - 1)
            {
                //
                // Csak magat a kort adjuk vissza.
                //

                string path = string.Join(" -> ", this.Skip(firstIndex).Select(sr => sr.RelatedServiceEntry).Select(IServiceIdExtensions.FriendlyName));

                throw new CircularReferenceException(string.Format
                (
                    Resources.Culture,
                    Resources.CIRCULAR_REFERENCE,
                    path 
                ));
            }
        }

        public IDisposable With(IServiceReference node) 
        {
            FGraph.Push(node);
            return new WithScope(FGraph);
        }

        private sealed class WithScope : Disposable 
        {
            private readonly Stack<IServiceReference> FGraph;

            public WithScope(Stack<IServiceReference> graph) => FGraph = graph;

            protected override void Dispose(bool disposeManaged)
            {
                FGraph.Pop();
                base.Dispose(disposeManaged);
            }
        }

        public IEnumerator<IServiceReference> GetEnumerator() =>
            //
            // Verem elemek felsorolasa forditva tortenik -> Reverse()
            //

            FGraph.Reverse().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
