/********************************************************************************
* DotGraph.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Text;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describe a <a href="https://graphviz.org/">DOT graph</a>.
    /// </summary>
    internal class DotGraph
    {
        private readonly HashSet<DotGraphNode> FNodes = new();
        public ICollection<DotGraphNode> Nodes => FNodes;

        private readonly HashSet<DotGraphEdge> FEdges = new();
        public ICollection<DotGraphEdge> Edges => FEdges;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("digraph G {");

            foreach (DotGraphNode node in Nodes)
            {
                node.Build(sb);
            }

            sb.AppendLine();

            foreach (DotGraphEdge edge in Edges)
            {
                edge.Build(sb);
            }

            sb.Append('}');

            return sb.ToString();
        }
    }
}
