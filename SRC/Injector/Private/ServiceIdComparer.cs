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
    /// <remarks>Don't specialize this comparer against <see cref="IServiceId"/> to let the compiler inline.</remarks>
    internal sealed class ServiceIdComparer<TServiceId> : ComparerBase<ServiceIdComparer<TServiceId>, TServiceId> where TServiceId : class, IServiceId
    {
        public override bool Equals(TServiceId x, TServiceId y) => x.Interface == y.Interface && x.Name == y.Name;

        public override int GetHashCode(TServiceId obj) => unchecked(obj.Interface.GetHashCode() ^ (obj.Name?.GetHashCode() ?? 0));
    }
}
