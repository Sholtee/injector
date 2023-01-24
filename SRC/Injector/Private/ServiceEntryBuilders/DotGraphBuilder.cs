/********************************************************************************
* DotGraphBuilder.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class DotGraphBuilder: IServiceEntryBuilder
    {
        private readonly ServicePath FPath = new();

        private readonly IServiceResolver FResolver;

        public DotGraph Graph { get; } = new();

        public DotGraphBuilder(IServiceResolver resolver)
        {
            Visitors = new IFactoryVisitor[]
            {
                new MergeProxiesVisitor(),
                new DotGraphBuilderVisitor(this)
            };
            FResolver = resolver;
        }

        public IReadOnlyList<IFactoryVisitor> Visitors { get; }

        public IBuildContext BuildContext { get; } = null!;

        public void Build(Type iface, string? name) => Build
        (
            FResolver.Resolve(iface, name) ?? new MissingServiceEntry(iface, name)
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
                Visitors.Aggregate<IFactoryVisitor, LambdaExpression>
                (
                    entry.Factory!,
                    (visited, visitor) => visitor.Visit(visited, entry)
                );
            }
            finally
            {
                FPath.Pop();
            }
        }
    }
}
