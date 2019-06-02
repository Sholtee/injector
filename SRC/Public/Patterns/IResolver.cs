/********************************************************************************
* IResolver.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Provides a mechanism for resolving implementations.
    /// </summary>
    public interface IResolver
    {
        /// <summary>
        /// Resolves the implementation of the given interface.
        /// </summary>
        /// <param name="interface">The service interface whose implementation is requested. It can be an open generic type. In this case the returned implementation must be also an open generic type.</param>
        /// <returns>The resolved service implementation.</returns>
        /// <remarks>Resolve can be called several times but never with the same <paramref name="interface"/> parameter.</remarks>
        Type Resolve(Type @interface);
    }
}
