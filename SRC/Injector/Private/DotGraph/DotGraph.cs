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
    using Primitives.Patterns;
    using Primitives.Threading;

    /// <summary>
    /// Describe a <a href="https://graphviz.org/">DOT graph</a>.
    /// </summary>
    internal class DotGraph
    {
        //
        // We cannot inherit from StringBuilder since it is sealed.
        //

        private sealed class PooledStringBuilder : IResettable
        {
            public StringBuilder StringBuilder { get; } = new();

            bool IResettable.Dirty => StringBuilder.Length > 0;

            void IResettable.Reset() => StringBuilder.Clear();
        }

        private static readonly ObjectPool<PooledStringBuilder> FPool = new(static () => new PooledStringBuilder(), Environment.ProcessorCount);

        public ISet<DotGraphNode> Nodes { get; } = new HashSet<DotGraphNode>();

        public ISet<DotGraphEdge> Edges { get; } = new HashSet<DotGraphEdge>();

        public override string ToString() => ToString(Environment.NewLine);

        public string ToString(string newLine)
        {
            using PoolItem<PooledStringBuilder> poolEntry = FPool.GetItem(CheckoutPolicy.Block)!;

            StringBuilder sb = poolEntry.Value.StringBuilder;
            sb.Append($"digraph G {{{newLine}");

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
