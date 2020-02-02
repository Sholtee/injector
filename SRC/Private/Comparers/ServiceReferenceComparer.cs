/********************************************************************************
* ServiceReferenceComparer.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceReferenceComparer : ComparerBase<ServiceReferenceComparer, ServiceReference>
    {
        public override int GetHashCode(ServiceReference obj) => obj.RelatedServiceEntry.GetHashCode();
    }
}
