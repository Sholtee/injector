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
    internal abstract class DotGraphElement
    {
        protected DotGraphElement(string id) => Id = id;

        /// <summary>
        /// Attributes related to this element. Principal attributes can be found <a href="https://graphviz.org/documentation/">here</a>.
        /// </summary>
        public IDictionary<string, string> Attributes { get; } = new Dictionary<string, string>();

        public string Id { get; }

        public override string ToString() => Attributes.Count > 0
            ? $"{Id} [{string.Join(",", Attributes.Select(static attr => $"{attr.Key}={attr.Value}"))}];"
            : $"{Id};";

        [SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity")]
        public override int GetHashCode() => Id.GetHashCode();

        public override bool Equals(object obj) => obj is DotGraphElement other && other.Id == Id;
    }
}
