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
        /// The service gets disposed only when its <b>type</b> is an <see cref="IDisposable"/> (or <see cref="IAsyncDisposable"/>) descendant.
        /// </summary>
        Soft,

        /// <summary>
        /// The service gets disposed when its <b>implementation</b> is an <see cref="IDisposable"/> (or <see cref="IAsyncDisposable"/>) descendant.
        /// </summary>
        Force,

        /// <summary>
        /// No disposal takes place.
        /// </summary>
        Suppress
    }
}
