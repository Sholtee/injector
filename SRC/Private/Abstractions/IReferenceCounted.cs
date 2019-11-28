/********************************************************************************
* IReferenceCounted.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface IReferenceCounted: IDisposable
    {
        /// <summary>
        /// Increments the reference counter as an atomic operation.
        /// </summary>
        /// <returns>The current reference count.</returns>
        int AddRef();

        /// <summary>
        /// Decrements the reference counter as an atomic operation and disposes the object if the reference count reaches the zero.
        /// </summary>
        /// <returns>The current reference count.</returns>
        int Release();
    }
}
