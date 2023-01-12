/********************************************************************************
* DotGraph.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describe a <a href="https://graphviz.org/">DOT graph</a>.
    /// </summary>
    internal class DotGraph
    {
        public ISet<DotGraphNode> Nodes { get; } = new HashSet<DotGraphNode>();

        public ISet<DotGraphEdge> Edges { get; } = new HashSet<DotGraphEdge>();

        public override string ToString() => ToString(Environment.NewLine);

        public string ToString(string newLine)
        {
            StringBuilder sb = new($"digraph G {{{newLine}");
 
            foreach (DotGraphNode node in Nodes)
            {
                sb.Append($"  {node}{newLine}");
            }

            sb.Append(newLine);

            foreach (DotGraphEdge edge in Edges)
            {
                sb.Append($"  {edge}{newLine}");
            }

            sb.Append('}');

            return sb.ToString();
        }
    }
}
