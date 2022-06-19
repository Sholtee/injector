/********************************************************************************
* DotGraphEdge.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal record DotGraphEdge : DotGraphElement
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

        public override string ToString() => $"{From.Id} -> {To.Id}{base.ToString()}";
    }
}
