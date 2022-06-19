/********************************************************************************
* DotGraphBuilder.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class DotGraphBuilder : ServiceEntryBuilder
    {
        public DotGraph Graph { get; }

        public IServiceEntryLookup Lookup { get; }

        public ServicePath Path { get; }

        public DotGraphBuilder(DotGraph graph, IServiceEntryLookup lookup)
        {
            Graph = graph;
            Lookup = lookup;
            Path = new ServicePath();
        }

        public override void Build(AbstractServiceEntry entry)
        {
            AbstractServiceEntry? parent = Path.Last;

            try
            {
                Path.Push(entry);
            }
            catch (CircularReferenceException cref)
            {
                HashSet<DotGraphNode> circle = new
                (
                    (
                        (IReadOnlyList<AbstractServiceEntry>) cref.Data[nameof(circle)]
                    ).Select(entry => new ServiceNode(entry))
                );

                foreach (DotGraphEdge edge in Graph.Edges)
                {
                    if (circle.Contains(edge.From) && circle.Contains(edge.To))
                        edge.Attributes["color"] = "red";
                }

                return;
            }

            //
            // Should not be within the firs try-catch block
            //

            try
            {
                Graph.Nodes.Add(new ServiceNode(entry));

                if (parent is not null)
                    Graph.Edges.Add(new DotGraphEdge(new ServiceNode(parent), new ServiceNode(entry)));

                //entry.VisitFactory(..., FactoryVisitorOptions.Default);
            }
            finally
            {
                Path.Pop();
            }
        }
    }
}
