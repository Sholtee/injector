/********************************************************************************
* IScopeFactory.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Provides a thread safe way to create new scopes.
    /// </summary>
    public interface IScopeFactory
    {
        /// <summary>
        /// Creates an independent new scope
        /// </summary>
        IInjector CreateScope(IReadOnlyDictionary<string, object>? options);

        /// <summary>
        /// Creates an independent new scope
        /// </summary>
        IInjector CreateScope(IServiceContainer parent, IReadOnlyDictionary<string, object>? options);
    }
}
