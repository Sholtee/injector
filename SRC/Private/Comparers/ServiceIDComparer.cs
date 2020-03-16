/********************************************************************************
* ServiceIdComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceIdComparer : ComparerBase<ServiceIdComparer, IServiceId>
    {
        public override int GetHashCode(IServiceId obj) =>
#if NETSTANDARD2_0
            new { obj.Interface, obj.Name }.GetHashCode()
#else
            System.HashCode.Combine(obj.Interface, obj.Name)
#endif
            ;
    }
}
