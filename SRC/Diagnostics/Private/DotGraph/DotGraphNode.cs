/********************************************************************************
* DotGraphNode.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Globalization;

namespace Solti.Utils.DI.Internals
{
    internal record DotGraphNode : DotGraphElement
    {
        private readonly int FId;

        //
        // Node azonositonak betuvel kell kezdodnie es egyedinek kell lennie.
        // 

        public string Id => $"N_{FId.ToString("X8", CultureInfo.InvariantCulture)}";

        public DotGraphNode(int id)
        {
            FId = id;
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

        public override string ToString() => Id + base.ToString();
    }
}
