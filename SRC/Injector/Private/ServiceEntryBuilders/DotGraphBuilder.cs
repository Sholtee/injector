/********************************************************************************
* DotGraphBuilder.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class DotGraphBuilder : ServiceEntryBuilder
    {
        private readonly IServiceEntryLookup FLookup;

        private readonly ServicePath FPath = new();

        private readonly GraphBuilderVisitor FVisitor;

        public DotGraph Graph { get; } = new();

        public DotGraphBuilder(IServiceEntryLookup lookup)
        {
            FLookup = lookup;
            FVisitor = new GraphBuilderVisitor(this);
        }

        public void BuildById(Type iface, string? name) => Build
        (
            FLookup.Get(iface, name) ?? new MissingServiceEntry(iface, name)
        );

        public override void Build(AbstractServiceEntry entry)
        {
            try
            {
                FPath.Push(entry);
            }
            catch (CircularReferenceException cref)
            {
                IReadOnlyList<AbstractServiceEntry> circle = (IReadOnlyList<AbstractServiceEntry>) cref.Data[nameof(circle)];

                foreach (ServiceEdge edge in Graph.Edges)
                {
                    if (circle.Contains(edge.From.RelatedEntry) && circle.Contains(edge.To.RelatedEntry))
                        edge.Attributes["color"] = "red";
                }

                return;
            }
   
            try
            {
                ServiceNode child = new(entry);

                Graph.Nodes.Add(child);

                if (FPath.Last is not null)
                    Graph.Edges.Add(new DotGraphEdge(new ServiceNode(FPath.Last), child));

                entry.VisitFactory(FVisitor.VisitLambda, FactoryVisitorOptions.Default);
            }
            finally
            {
                FPath.Pop();
            }
        }
    }
}
