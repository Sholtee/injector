/********************************************************************************
* DotGraphNode.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Globalization;

namespace Solti.Utils.DI.Internals
{
    internal class DotGraphNode : DotGraphElement
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
            get => (string) Attributes["label"];
            set => Attributes["label"] = $"<{value}>";
        }

        public override int GetHashCode() => FId;

        public override bool Equals(object obj) => obj is DotGraphNode node && node.FId == FId;
    
        public override string ToString() => Id + base.ToString();
    }
}
