/********************************************************************************
* DotGraphEdge.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Text;

namespace Solti.Utils.DI.Internals
{
    internal class DotGraphEdge : DotGraphElement
    {
        public DotGraphEdge(DotGraphNode from, DotGraphNode to)
        {
            From = from;
            To = to;
            Attributes["style"] = "normal";
        }

        public DotGraphNode From { get; }

        public DotGraphNode To { get; }

        public override void Build(StringBuilder stringBuilder)
        {
            stringBuilder.Append($"  {this}");
            base.Build(stringBuilder);
        }

        public override int GetHashCode() => new { From, To }.GetHashCode();

        public override bool Equals(object obj) => obj is DotGraphEdge edge && edge.GetHashCode() == GetHashCode();

        public override string ToString() => $"{From} -> {To}";
    }
}
