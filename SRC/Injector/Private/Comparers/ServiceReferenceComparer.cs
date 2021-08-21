/********************************************************************************
* ServiceReferenceComparer.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class ServiceReferenceComparer : ComparerBase<ServiceReferenceComparer, IServiceReference>
    {
        public override bool Equals(IServiceReference x, IServiceReference y) => ServiceIdComparer.Instance.Equals(x.RelatedServiceEntry, y.RelatedServiceEntry);

        public override int GetHashCode(IServiceReference obj) => ServiceIdComparer.Instance.GetHashCode(obj.RelatedServiceEntry);
    }
}
