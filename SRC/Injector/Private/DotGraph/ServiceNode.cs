/********************************************************************************
* ServiceNode.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceNode : DotGraphNode
    {
        public AbstractServiceEntry RelatedEntry { get; }

        public ServiceNode(AbstractServiceEntry entry) : base(entry.ToString(shortForm: true))
        {
            RelatedEntry = entry;
            Label = $"<u>{entry.ToString(shortForm: true)}</u><br/><br/><i>{entry.Lifetime?.ToString() ?? "NULL"}</i>";

            if (entry is MissingServiceEntry)
                Attributes["fontcolor"] = Attributes["color"] = "red";
        }
    }
}
