/********************************************************************************
* DotGraphNode.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Text.RegularExpressions;

namespace Solti.Utils.DI.Internals
{
    internal class DotGraphNode : DotGraphElement
    {
        //
        // See https://graphviz.org/doc/info/lang.html
        //

        private static readonly Regex FIncompatibleChars = new("[^0-9a-zA-Z_\\200-\\377]", RegexOptions.Compiled);

        //
        // Id must begin with a letter and cannot contain spaces.
        //

        public DotGraphNode(string id): base($"N_{FIncompatibleChars.Replace(id, "_")}")
        {
            Attributes["shape"] = "box";
            Attributes["margin"] = ".1";
        }

        /// <summary>
        /// Label that accepts HTML like elements.
        /// </summary>
        public string Label
        {
            get => Attributes["label"];
            set => Attributes["label"] = $"<{value}>";
        }
    }
}
