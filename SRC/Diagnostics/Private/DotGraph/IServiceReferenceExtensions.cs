/********************************************************************************
* IServiceReferenceExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class IServiceReferenceExtensions
    {
        public static DotGraphNode AsDotGraphNode(this IServiceReference serviceReference)
        {
            string friendlyName = serviceReference.RelatedServiceEntry.FriendlyName();

            return new DotGraphNode
            (
                friendlyName.GetHashCode
                (
#if !NETSTANDARD2_0
                    System.StringComparison.OrdinalIgnoreCase
#endif
                )
            )
            {
                Label = $"<u>{friendlyName}</u><br/><br/><i>{serviceReference.RelatedServiceEntry.Lifetime}</i>"
            };
        }

        public static DotGraph AsDotGraph(this IServiceReference serviceReference)
        {
            DotGraph graph = new();
            Process(serviceReference);
            return graph;

            void Process(IServiceReference serviceReference)
            {
                DotGraphNode from = serviceReference.AsDotGraphNode();
                graph.Nodes.Add(from);

                foreach (IServiceReference dep in serviceReference.Dependencies)
                {
                    DotGraphNode to = dep.AsDotGraphNode();

                    DotGraphEdge edge = new(from, to);

                    //
                    // Korkoros referencia eseten ne legyen S.O.E.
                    //

                    if (!graph.Edges.Add(edge))
                        continue;

                    graph.Nodes.Add(to);

                    Process(dep);
                }
            }
        }
    }
}
