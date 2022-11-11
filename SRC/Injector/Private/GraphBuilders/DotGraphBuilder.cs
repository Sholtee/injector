/********************************************************************************
* DotGraphBuilder.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class DotGraphBuilder: IGraphBuilder
    {
        private readonly IServiceResolverLookup FLookup;

        private readonly ServicePath FPath = new();

        private readonly DotGraphBuilderVisitor FVisitor;

        public DotGraph Graph { get; } = new();

        public DotGraphBuilder(IServiceResolverLookup lookup)
        {
            FLookup = lookup;
            FVisitor = new DotGraphBuilderVisitor(this);
        }

        public void Build(Type iface, string? name) => Build
        (
            FLookup.Get(iface, name)?.GetUnderlyingEntry() ?? new MissingServiceEntry(iface, name)
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

            if (!entry.Features.HasFlag(ServiceEntryFeatures.SupportsBuild))
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
                entry.Build(null, FVisitor);
            }
            finally
            {
                FPath.Pop();
            }
        }
    }
}
