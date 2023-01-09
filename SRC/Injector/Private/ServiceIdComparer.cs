/********************************************************************************
* ServiceIdComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class ServiceIdComparer : ComparerBase<ServiceIdComparer, IServiceId>, IComparer<IServiceId>
    {
        public override bool Equals(IServiceId x, IServiceId y) => x?.Interface == y?.Interface && x?.Name == y?.Name;

        public override int GetHashCode(IServiceId obj) => unchecked((obj?.Interface.GetHashCode() ?? 0) ^ (obj?.Name?.GetHashCode() ?? 0));

        public int Compare(IServiceId x, IServiceId y)
        {
            //
            // We have to return Int32 -> Math.Sign()
            //

            int order = Math.Sign((long) x.Interface.TypeHandle.Value - (long) y.Interface.TypeHandle.Value);
            if (order is 0)
                //
                // StringComparer supports NULL despite it is not reflected by nullable annotation
                //

                order = StringComparer.InvariantCultureIgnoreCase.Compare(x.Name, y.Name);
            return order;
        }
    }
}
