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

    internal sealed class DotGraphBuilder : IServiceEntryBuilder
    {
        private readonly IServiceResolverLookup FLookup;

        private readonly ServicePath FPath = new();

        private readonly GraphBuilderVisitor FVisitor;

        public DotGraph Graph { get; } = new();

        public DotGraphBuilder(IServiceResolverLookup lookup)
        {
            FLookup = lookup;
            FVisitor = new GraphBuilderVisitor(this);
        }

        public void BuildById(Type iface, string? name) => Build
        (
            FLookup.Get(iface, name)?.RelatedEntry ?? new MissingServiceEntry(iface, name)
        );

        public void Build(AbstractServiceEntry entry)
        {
            ServiceNode child = new(entry);

            if (FPath.Last is not null)
                Graph.Edges.Add
                (
                    new ServiceEdge
                    (
                        new ServiceNode(FPath.Last),
                        child
                    )
                );

            Graph.Nodes.Add(child);

            if (!entry.Features.HasFlag(ServiceEntryFeatures.SupportsVisit))
                return;

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
                        edge.MarkRed();
                }

                return;
            }
   
            try
            {
                entry.VisitFactory(FVisitor.VisitLambda, null);
            }
            finally
            {
                FPath.Pop();
            }
        }
    }
}
