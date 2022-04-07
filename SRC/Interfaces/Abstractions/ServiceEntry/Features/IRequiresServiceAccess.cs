/********************************************************************************
* IRequiresServiceAccess.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Implies that the <see cref="AbstractServiceEntry.Factory"/> function returns a wrapped service instance that has to be extracted.
    /// </summary>
    public interface IRequiresServiceAccess // TODO: remove
    {
        /// <summary>
        /// Contains the accessor function that extracts the effective service instance from the value returned by the <see cref="AbstractServiceEntry.Factory"/> function.
        /// </summary>
        /// <remarks>Disposal logic is always applied against the original value (returned by the <see cref="AbstractServiceEntry.Factory"/> function).</remarks>
        public Func<object, object> ServiceAccess { get; }
    }
}
