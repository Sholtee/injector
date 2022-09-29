/********************************************************************************
* DotGraphServiceEntryVisitor.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class DotGraphServiceEntryVisitor : IServiceEntryVisitor
    {
        private readonly IServiceResolverLookup FLookup;

        private readonly ServicePath FPath = new();

        private readonly GraphBuilderVisitor FVisitor;

        public DotGraph Graph { get; } = new();

        public DotGraphServiceEntryVisitor(IServiceResolverLookup lookup)
        {
            FLookup = lookup;
            FVisitor = new GraphBuilderVisitor(this);
        }

        public void BuildById(Type iface, string? name) => Visit
        (
            FLookup.Get(iface, name)?.RelatedEntry ?? new MissingServiceEntry(iface, name)
        );

        public void Visit(AbstractServiceEntry entry)
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
                foreach (ServiceEdge edge in Graph.Edges)
                {
                    if (cref.Circle.Contains(edge.From.RelatedEntry) && cref.Circle.Contains(edge.To.RelatedEntry))
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
