/********************************************************************************
* DotGraphElement.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes an abstract <a href="https://graphviz.org/">DOT graph</a> element
    /// </summary>
    internal abstract record DotGraphElement
    {
        /// <summary>
        /// Attributes related to this element. Principal attributes can be found <a href="https://graphviz.org/pdf/dotguide.pdf">here</a>.
        /// </summary>
        protected IDictionary<string, string> Attributes { get; } = new AttributeCollection();

        public override string ToString() => Attributes.ToString();

        private sealed class AttributeCollection : Dictionary<string, string>
        {
            [SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity")]
            public override int GetHashCode() => ToString().GetHashCode();

            public override bool Equals(object obj) => (obj as AttributeCollection)?.ToString() == ToString();

            public override string ToString() => Count > 0
                ? $" [{string.Join(",", this.Select(static attr => $"{attr.Key}={attr.Value}"))}];"
                : ";";
        }
    }
}
