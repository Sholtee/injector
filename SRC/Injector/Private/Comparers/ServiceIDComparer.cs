/********************************************************************************
* ServiceIdComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class ServiceIdComparer : ComparerBase<ServiceIdComparer, IServiceId> // TODO: torolni
    {
        public override bool Equals(IServiceId x, IServiceId y) => x.Interface == y.Interface && x.Name == y.Name;

        public override int GetHashCode(IServiceId obj) =>
#if NETSTANDARD2_1_OR_GREATER
            HashCode.Combine(obj.Interface, obj.Name)
#else
            new { obj.Interface, obj.Name }.GetHashCode()        
#endif
            ;
    }
}
