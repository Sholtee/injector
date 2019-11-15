/********************************************************************************
* IDisposableEx.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Extends the <see cref="IDisposable"/> interface.
    /// </summary>
    public interface IDisposableEx: IDisposable
    {
        /// <summary>
        /// Indicates that the object was disposed or not.
        /// </summary>
        bool Disposed { get; }
    }
}