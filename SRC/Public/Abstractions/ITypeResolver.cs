/********************************************************************************
* ITypeResolver.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Provides the mechanism of resolving types.
    /// </summary>
    public interface ITypeResolver
    {
        /// <summary>
        /// Returns true if the resolver supports the given interface, false otherwise.
        /// </summary>
        /// <param name="interface">The interface to be checked.</param>
        bool Supports(Type @interface);

        /// <summary>
        /// Resolves the implementation of the given interface.
        /// </summary>
        /// <param name="interface">The service interface whose implementation is requested. It can be an open generic type. In this case the returned implementation must also be an open generic type.</param>
        /// <returns>The resolved service implementation.</returns>
        /// <exception cref="NotSupportedException">In case of unsupported interface.</exception>
        Type Resolve(Type @interface);
    }
}
