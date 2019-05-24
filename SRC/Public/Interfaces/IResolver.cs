/********************************************************************************
* IResolver.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    public interface IResolver
    {
        /// <summary>
        /// Resolves the implementation of the given interface.
        /// </summary>
        Type Resolve(Type @interface);
    }
}
