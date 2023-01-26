/********************************************************************************
* ServiceDisposalMode.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Contains the possible disposal modes.
    /// </summary>
    public enum ServiceDisposalMode
    {
        /// <summary>
        /// The service gets disposed only when its interface is an <see cref="IDisposable"/> descendant.
        /// </summary>
        Default,

        /// <summary>
        /// The service gets disposed if it can be casted to <see cref="IDisposable"/> regardlesss its interfce.
        /// </summary>
        Force,

        /// <summary>
        /// No disposal takes place.
        /// </summary>
        Suppress
    }
}
