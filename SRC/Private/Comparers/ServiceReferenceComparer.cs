/********************************************************************************
* ServiceReferenceComparer.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceReferenceComparer : ComparerBase<ServiceReferenceComparer, AbstractServiceReference>
    {
        public override int GetHashCode(AbstractServiceReference obj) => obj.RelatedServiceEntry.GetHashCode();
    }
}
