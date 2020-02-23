/********************************************************************************
* IHasUnderlyingImplementation.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Exposes the underlying implementation of a service. 
    /// </summary>
    public interface IHasUnderlyingImplementation
    {
        /// <summary>
        /// The underlying implementation of the service.
        /// </summary>
        object UnderlyingImplementation { get; }
    }
}
