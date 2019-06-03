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
        /// <param name="interface">The service interface whose implementation is requested. It can be an open generic type. In this case the returned implementation must also be an open generic type.</param>
        /// <returns>The resolved service implementation.</returns>
        Type Resolve(Type @interface);
    }
}
