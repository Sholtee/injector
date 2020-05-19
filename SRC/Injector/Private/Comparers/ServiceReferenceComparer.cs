/********************************************************************************
* ServiceReferenceComparer.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Primitives;

    internal sealed class ServiceReferenceComparer : ComparerBase<ServiceReferenceComparer, ServiceReference>
    {
        public override int GetHashCode(ServiceReference obj) => ServiceIdComparer.Instance.GetHashCode(obj.RelatedServiceEntry);
    }
}
