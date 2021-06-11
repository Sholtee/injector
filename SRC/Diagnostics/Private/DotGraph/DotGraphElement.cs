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
    internal abstract record DotGraphElement
    {
        /// <summary>
        /// Attributes related to this element. Principal attribute can be found <a href="https://graphviz.org/pdf/dotguide.pdf">here</a>.
        /// </summary>
        protected IDictionary<string, string> Attributes { get; } = new AttributeCollection();

        public override string ToString() => Attributes.Count > 0
            ? $" [{string.Join(",", Attributes.Select(attr => $"{attr.Key}={attr.Value}"))}];"
            : ";";

        private sealed class AttributeCollection : Dictionary<string, string>
        {
            public override int GetHashCode()
            {
                //
                // HashCode struct nincs netstandard2_0 alatt
                //

                object? current = null;

                foreach (var kvp in this)
                {
                    current = new { current, kvp.Key, kvp.Value }; // anonim objektum "record"-kent viselkedik
                }

                return current?.GetHashCode() ?? 0;
            }

            public override bool Equals(object obj) => obj is AttributeCollection coll && coll.GetHashCode() == GetHashCode();
        }
    }
}
