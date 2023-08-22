/********************************************************************************
* ILazy.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Lazily resolves a dependency
    /// </summary>
    /// <typeparam name="TInterfce">The service interface</typeparam>
    /// <remarks>Prefer this way for lazy resolution over <see cref="Lazy{T}"/></remarks>
    public interface ILazy<TInterfce>
    {
        /// <summary>
        /// The value associated to this instance. Created only once on the first request.
        /// </summary>
        TInterfce Value { get; }

        /// <summary>
        /// Returns true if the <see cref="Value"/> already assigned, false otherwise.
        /// </summary>
        bool IsValueCreated { get; }
    }
}