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
    /// <remarks>This enum is used by the <see cref="ServiceOptions.DisposalMode"/> property.</remarks>
    public enum ServiceDisposalMode
    {
        /// <summary>
        /// The service gets disposed only when its interface is an <see cref="IDisposable"/> descendant.
        /// </summary>
        Soft,

        /// <summary>
        /// The service gets disposed when the implementation is an <see cref="IDisposable"/> descendant.
        /// </summary>
        Force,

        /// <summary>
        /// No disposal takes place.
        /// </summary>
        Suppress
    }
}
