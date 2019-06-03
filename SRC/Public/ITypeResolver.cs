/********************************************************************************
* ITypeResolver.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Provides a mechanism for resolving types.
    /// </summary>
    public interface ITypeResolver
    {
        /// <summary>
        /// Resolves the implementation of the given interface.
        /// </summary>
        /// <param name="interface">The service interface whose implementation is requested. It can be an open generic type. In this case the returned implementation must be also an open generic type.</param>
        /// <returns>The resolved service implementation.</returns>
        /// <remarks>This method can be called several times but never with the same <paramref name="interface"/> parameter.</remarks>
        Type Resolve(Type @interface);
    }
}
