/********************************************************************************
* ServiceIdComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class ServiceIdComparer : ComparerBase<ServiceIdComparer, AbstractServiceEntry>
    {
        public override bool Equals(AbstractServiceEntry x, AbstractServiceEntry y) => x?.Interface == y?.Interface && x?.Name == y?.Name;

        public override int GetHashCode(AbstractServiceEntry obj) => unchecked((obj?.Interface.GetHashCode() ?? 0) ^ (obj?.Name?.GetHashCode() ?? 0));
    }
}
