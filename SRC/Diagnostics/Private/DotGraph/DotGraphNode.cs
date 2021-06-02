/********************************************************************************
* DotGraphNode.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Solti.Utils.DI.Internals
{
    internal class DotGraphNode : DotGraphElement
    {
        public int Id { get; }

        public DotGraphNode(int id)
        {
            Id = id;
            Attributes["shape"] = "box";
        }

        /// <summary>
        /// Label that accepts HTML like elements.
        /// </summary>
        public string Label 
        {
            get => (string) Attributes["label"];
            set => Attributes["label"] = $"<{value}>";
        }

        public override void Build(StringBuilder stringBuilder)
        {
            stringBuilder.Append($"  {this}");

            base.Build(stringBuilder);
        }

        public override int GetHashCode() => Id;

        public override bool Equals(object obj) => obj is DotGraphNode node && node.Id == Id;


        [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
        public override string ToString() => Id.ToString("x8");
    }
}
