/********************************************************************************
* ServiceIdComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Primitives;

    internal sealed class ServiceIdComparer : ComparerBase<ServiceIdComparer, IServiceId>
    {
        public override int GetHashCode(IServiceId obj) =>
#if NETSTANDARD2_0
            new { obj.Interface, obj.Name }.GetHashCode()
#else
            HashCode.Combine(obj.Interface, obj.Name)
#endif
            ;
    }
}
