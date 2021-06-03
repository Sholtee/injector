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
        public ICollection<DotGraphNode> Nodes { get; } = new HashSet<DotGraphNode>();

        public ICollection<DotGraphEdge> Edges { get; } = new HashSet<DotGraphEdge>();

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine("digraph G {");

            foreach (DotGraphNode node in Nodes)
            {
                sb.AppendLine($"  {node}");
            }

            sb.AppendLine();

            foreach (DotGraphEdge edge in Edges)
            {
                sb.AppendLine($"  {edge}");
            }

            sb.Append('}');

            return sb.ToString();
        }
    }
}
