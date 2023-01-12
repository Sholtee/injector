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

    internal sealed class DotGraphBuilder: IServiceEntryBuilder
    {
        private readonly ServicePath FPath = new();

        private readonly IServiceEntryLookup FLookup;

        public DotGraph Graph { get; } = new();

        public DotGraphBuilder(IServiceEntryLookup lookup)
        {
            Visitors = new IFactoryVisitor[]
            {
                new DotGraphBuilderVisitor(this)
            };
            FLookup = lookup;
        }

        public IFactoryVisitor[] Visitors { get; }

        public IBuildContext BuildContext { get; } = null!;

        public void Build(Type iface, string? name) => Build
        (
            FLookup.Get(iface, name) ?? new MissingServiceEntry(iface, name)
        );

        public void Build(AbstractServiceEntry entry)
        {
            ServiceNode child = new(entry);

            if (FPath.Last is not null)
            {
                Graph.Edges.Add
                (
                    new ServiceEdge
                    (
                        new ServiceNode(FPath.Last),
                        child
                    )
                );
            }

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
                    {
                        edge.MarkRed();
                    }
                }
                return;
            }

            try
            {
                Visitors.Single().Visit(entry.Factory!, null!);
            }
            finally
            {
                FPath.Pop();
            }
        }
    }
}
