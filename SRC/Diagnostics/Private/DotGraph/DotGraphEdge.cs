/********************************************************************************
* DotGraphEdge.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal class DotGraphEdge : DotGraphElement
    {
        public DotGraphEdge(DotGraphNode from, DotGraphNode to)
        {
            From = from;
            To = to;
            Attributes["style"] = "normal";
            //Attributes["arrowhead"] = "none";
        }

        public DotGraphNode From { get; }

        public DotGraphNode To { get; }

        public override int GetHashCode() => new { From, To }.GetHashCode();

        public override bool Equals(object obj) => obj is DotGraphEdge edge && edge.GetHashCode() == GetHashCode();

        public override string ToString() => $"{From.Id} -> {To.Id}{base.ToString()}";
    }
}
