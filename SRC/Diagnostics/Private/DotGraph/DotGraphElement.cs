/********************************************************************************
* DotGraphElement.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

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

        public override string ToString() => Attributes.Count > 0
            ? $" [{string.Join(",", Attributes.Select(attr => $"{attr.Key}={attr.Value}"))}];"
            : ";";
    }
}
