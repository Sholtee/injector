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
        private readonly ServicePath FPath = new();

        private readonly DotGraphBuilderVisitor FVisitor;

        public DotGraph Graph { get; } = new();

        public DotGraphBuilder(IServiceEntryLookup lookup)
        {
            FVisitor = new DotGraphBuilderVisitor(this);
            Lookup = lookup;
        }

        public void Build(Type iface, string? name) => Build
        (
            Lookup.Get(iface, name) ?? new MissingServiceEntry(iface, name)
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
                entry.Build(null, null!, FVisitor);
            }
            finally
            {
                FPath.Pop();
            }
        }

        public IServiceEntryLookup Lookup { get; }

        public int Slots => throw new NotSupportedException();
    }
}
