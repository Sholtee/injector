/********************************************************************************
* ServiceIdComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;

    /// <summary>
    /// Specifies how to compare <see cref="AbstractServiceEntry"/> instances considering their interface and name only.
    /// </summary>
    public sealed class ServiceIdComparer : ComparerBase<ServiceIdComparer, AbstractServiceEntry>
    {
        /// <summary>
        /// See <see cref="ComparerBase{TConcreteComparer, T}.Equals(T, T)"/>
        /// </summary>
        public override bool Equals(AbstractServiceEntry x, AbstractServiceEntry y) => x?.Interface == y?.Interface && x?.Name == y?.Name;

        /// <summary>
        /// See <see cref="ComparerBase{TConcreteComparer, T}.GetHashCode(T)"/>
        /// </summary>
        [SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity")]
        public override int GetHashCode(AbstractServiceEntry obj) => unchecked((obj?.Interface.GetHashCode() ?? 0) ^ (obj?.Name?.GetHashCode() ?? 0));
    }
}
