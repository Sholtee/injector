/********************************************************************************
* ServiceIdComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    /// <summary>
    /// ServiceId comparer
    /// </summary>
    internal sealed class ServiceIdComparer : ComparerBase<ServiceIdComparer, IServiceId>
    {
        public override bool Equals(IServiceId x, IServiceId y) => x.Interface == y.Interface && x.Name == y.Name;

        public override int GetHashCode(IServiceId obj) => unchecked(obj.Interface.GetHashCode() ^ (obj.Name?.GetHashCode() ?? 0));
    }
}
