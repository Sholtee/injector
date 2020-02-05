/********************************************************************************
* ServiceIDComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceIDComparer : ComparerBase<ServiceIDComparer, IServiceID>
    {
        public override int GetHashCode(IServiceID obj) =>
#if NETSTANDARD1_6 || NETSTANDARD2_0
            new { obj.Interface, obj.Name }.GetHashCode()
#else
            HashCode.Combine(obj.Interface, obj.Name)
#endif
            ;
    }
}
