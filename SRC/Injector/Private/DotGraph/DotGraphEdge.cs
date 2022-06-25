/********************************************************************************
* DotGraphEdge.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal class DotGraphEdge : DotGraphElement
    {
        public DotGraphEdge(DotGraphNode from, DotGraphNode to): base($"{from.Id} -> {to.Id}")
        {
            From = from;
            To = to;
            Attributes["style"] = "normal";
            //Attributes["arrowhead"] = "none";
        }

        public DotGraphNode From { get; }

        public DotGraphNode To { get; }
    }
}
