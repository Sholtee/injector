/********************************************************************************
* DotGraphElement.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes an abstract <a href="https://graphviz.org/">DOT graph</a> element
    /// </summary>
    internal abstract class DotGraphElement
    {
        /// <summary>
        /// Attributes related to this element. Principal attribute can be found <a href="https://graphviz.org/pdf/dotguide.pdf">here</a>.
        /// </summary>
        protected IDictionary<string, object> Attributes { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Builds this element.
        /// </summary>
        public virtual void Build(StringBuilder stringBuilder)
        {
            if (Attributes.Count > 0)
                stringBuilder.Append($" [{string.Join(",", Attributes.Select(attr => $"{attr.Key}={attr.Value}"))}]");

            stringBuilder.AppendLine(";");
        }
    }
}
