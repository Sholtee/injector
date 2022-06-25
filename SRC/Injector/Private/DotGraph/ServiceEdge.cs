/********************************************************************************
* DotGraphEdge.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceEdge : DotGraphEdge
    {
        public ServiceEdge(ServiceNode from, ServiceNode to) : base(from, to)
        {
        }

        public new ServiceNode From => (ServiceNode) base.From;

        public new ServiceNode To => (ServiceNode) base.To;

        public void MarkRed() => Attributes["color"] = "red";
    }
}
