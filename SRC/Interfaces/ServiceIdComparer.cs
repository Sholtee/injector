/********************************************************************************
* ServiceIdComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;

    /// <summary>
    /// Specifies how to compare <see cref="IServiceId"/> instances.
    /// </summary>
    public sealed class ServiceIdComparer : ComparerBase<ServiceIdComparer, IServiceId>
    {
        /// <inheritdoc/>
        public override bool Equals(IServiceId x, IServiceId y) => x?.Interface == y?.Interface && x?.Name == y?.Name;

        /// <inheritdoc/>
        public override int GetHashCode(IServiceId obj) =>
#if NETSTANDARD2_1_OR_GREATER
            HashCode.Combine(obj?.Interface, obj?.Name)
#else
            new { obj?.Interface, obj?.Name }.GetHashCode()        
#endif
            ;
    }
}
